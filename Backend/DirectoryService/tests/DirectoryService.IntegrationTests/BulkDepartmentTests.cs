using System.Net;
using System.Net.Http.Json;
using System.Text;
using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Cache;
using DirectoryService.Application.Abstractions.Database;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Application.Features.Departments.Bulk;
using DirectoryService.Contracts.Common;
using DirectoryService.Contracts.Departments;
using DirectoryService.Domain.Entities;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.Shared;
using DirectoryService.Presentation.ApiResponse;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public sealed class BulkDepartmentTests : DirectoryServiceTestsBase
{
    public BulkDepartmentTests(DirectoryServiceWebFactory factory)
        : base(factory)
    {
    }

    public static IEnumerable<object[]> InvalidRequests()
    {
        foreach (var action in new[] { "archive", "restore", "activity" })
        {
            foreach (var body in new[]
                     {
                         "{}",
                         "{\"ids\":null}",
                         "{\"ids\":[]}",
                         $"{{\"ids\":[\"{Guid.Empty}\"]}}",
                         "{\"ids\":[\"not-a-guid\"]}",
                     })
            {
                yield return [action, action == "activity" ? body.Replace("}", ",\"isActive\":false}") : body];
            }
        }
    }

    [Fact]
    public async Task Activity_PartialSuccess_ShouldCommitValidIdsAndInvalidateCache()
    {
        var first = await SeedDepartmentAsync("First", "bulkfirst");
        var second = await SeedDepartmentAsync("Second", "bulksecond");
        var missing = Guid.NewGuid();
        using var client = CreateClient();
        (await ReadList(client)).Items.Should().OnlyContain(item => item.IsActive);

        var result = await SendBulk(client, "activity", [first.Value, missing, second.Value], false);

        result.SucceededIds.Should().BeEquivalentTo([first.Value, second.Value]);
        result.SucceededCount.Should().Be(2);
        result.FailedCount.Should().Be(1);
        result.Errors.Should().ContainSingle(error => error.Id == missing && error.Code == "department.not_found");
        var list = await ReadList(client);
        list.Items.Should().HaveCount(2).And.OnlyContain(item => !item.IsActive && item.DeletedAt == null);
    }

    [Theory]
    [InlineData("archive")]
    [InlineData("restore")]
    [InlineData("activity")]
    public async Task AllMissing_ShouldReturnSuccessfulEnvelopeWithPerItemErrors(string action)
    {
        Guid[] ids = [Guid.NewGuid(), Guid.NewGuid()];
        using var client = CreateClient();

        var result = await SendBulk(client, action, ids, false);

        result.SucceededIds.Should().BeEmpty();
        result.SucceededCount.Should().Be(0);
        result.FailedCount.Should().Be(2);
        result.Errors.Select(error => error.Id).Should().BeEquivalentTo(ids);
        result.Errors.Should().OnlyContain(error => error.Code == "department.not_found" && error.Message.Length > 0);
    }

    [Fact]
    public async Task DeactivateHierarchy_ShouldProcessDescendantsFirstRegardlessOfInputOrder()
    {
        var root = await SeedDepartmentAsync("Root", "bulkroot");
        var child = await SeedDepartmentAsync("Child", "bulkchild", root);
        var grandchild = await SeedDepartmentAsync("Grandchild", "bulkgrandchild", child);
        using var client = CreateClient();

        var result = await SendBulk(client, "activity", [root.Value, child.Value, grandchild.Value], false);

        result.Errors.Should().BeEmpty();
        result.SucceededIds.Should().Equal(grandchild.Value, child.Value, root.Value);
        await ExecuteDbContextAsync(async db =>
        {
            var departments = await db.Departments.IgnoreQueryFilters().ToListAsync();
            departments.Should().HaveCount(3).And.OnlyContain(item => !item.IsActive && item.DeletedAt == null);
            departments.Single(item => item.Id == grandchild).Path.Should().Be("bulkroot.bulkchild.bulkgrandchild");
        });
    }

    [Fact]
    public async Task ActivateHierarchy_ShouldProcessAncestorsFirstRegardlessOfInputOrder()
    {
        var root = await SeedDepartmentAsync("Root", "bulkroot");
        var child = await SeedDepartmentAsync("Child", "bulkchild", root);
        var grandchild = await SeedDepartmentAsync("Grandchild", "bulkgrandchild", child);
        await ExecuteDbContextAsync(db => db.Departments.ExecuteUpdateAsync(
            setters => setters.SetProperty(item => item.IsActive, false)));
        using var client = CreateClient();

        var result = await SendBulk(client, "activity", [grandchild.Value, child.Value, root.Value], true);

        result.Errors.Should().BeEmpty();
        result.SucceededIds.Should().Equal(root.Value, child.Value, grandchild.Value);
        (await ReadList(client)).Items.Should().HaveCount(3).And.OnlyContain(item => item.IsActive);
    }

    [Fact]
    public async Task Deactivate_ShouldReportActiveDescendantsAndStillProcessOtherDepartments()
    {
        var root = await SeedDepartmentAsync("Root", "blockedroot");
        var child = await SeedDepartmentAsync("Child", "blockedchild", root);
        var other = await SeedDepartmentAsync("Other", "otherroot");
        using var client = CreateClient();

        var result = await SendBulk(client, "activity", [root.Value, other.Value], false);

        result.SucceededIds.Should().ContainSingle().Which.Should().Be(other.Value);
        result.Errors.Should().ContainSingle(error =>
            error.Id == root.Value && error.Code == "department.activity.active_descendants");
        await ExecuteDbContextAsync(async db =>
        {
            (await db.Departments.SingleAsync(item => item.Id == root)).IsActive.Should().BeTrue();
            (await db.Departments.SingleAsync(item => item.Id == child)).IsActive.Should().BeTrue();
            (await db.Departments.IgnoreQueryFilters().SingleAsync(item => item.Id == other)).IsActive.Should().BeFalse();
        });
    }

    [Fact]
    public async Task Activate_ShouldNotImplicitlyActivateUnselectedAncestors()
    {
        var root = await SeedDepartmentAsync("Root", "inactiveroot");
        var child = await SeedDepartmentAsync("Child", "inactivechild", root);
        var other = await SeedDepartmentAsync("Other", "inactiveother");
        await ExecuteDbContextAsync(db => db.Departments.ExecuteUpdateAsync(
            setters => setters.SetProperty(item => item.IsActive, false)));
        using var client = CreateClient();

        var result = await SendBulk(client, "activity", [child.Value, other.Value], true);

        result.SucceededIds.Should().ContainSingle().Which.Should().Be(other.Value);
        result.Errors.Should().ContainSingle(error =>
            error.Id == child.Value && error.Code == "department.activity.parent_inactive");
        await ExecuteDbContextAsync(async db =>
        {
            (await db.Departments.IgnoreQueryFilters().SingleAsync(item => item.Id == root)).IsActive.Should().BeFalse();
            (await db.Departments.IgnoreQueryFilters().SingleAsync(item => item.Id == child)).IsActive.Should().BeFalse();
        });
    }

    [Fact]
    public async Task Activity_AlreadyAtTarget_ShouldSucceedButNeverRestoreArchivedDepartment()
    {
        var active = await SeedDepartmentAsync("Active", "alreadyactive");
        var archived = await SeedDepartmentAsync("Archived", "alreadyarchived");
        using var client = CreateClient();
        (await client.DeleteAsync($"/api/departments/{archived.Value}")).EnsureSuccessStatusCode();

        for (var iteration = 0; iteration < 2; iteration++)
        {
            var result = await SendBulk(client, "activity", [active.Value, archived.Value], true);
            result.SucceededIds.Should().ContainSingle().Which.Should().Be(active.Value);
            result.Errors.Should().ContainSingle(error =>
                error.Id == archived.Value && error.Code == "department.activity.archived");
        }
    }

    [Fact]
    public async Task ArchiveAndRestoreHierarchy_ShouldPreservePathsLinksAndSingleOperationSideEffects()
    {
        var location = await SeedLocationAsync("Shared Office");
        var root = await SeedDepartmentAsync("Root", "archiveroot", locationIds: [location]);
        var child = await SeedDepartmentAsync("Child", "archivechild", root, [location]);
        var grandchild = await SeedDepartmentAsync("Grandchild", "archivegrandchild", child);
        var position = await SeedPositionAsync("Shared Position", [root, child]);
        using var client = CreateClient();
        (await ReadList(client)).Items.Should().HaveCount(3);
        var locationsBefore = await client.GetFromJsonAsync<Envelope<PagedList<DirectoryService.Contracts.Locations.LocationListItemDto>>>(
            "/api/locations");
        locationsBefore!.Result!.Items.Should().Contain(item => item.Id == location.Value);

        var archive = await SendBulk(client, "archive", [root.Value, child.Value]);

        archive.SucceededIds.Should().Equal(child.Value, root.Value);
        archive.Errors.Should().BeEmpty();
        (await ReadList(client)).Items.Should().ContainSingle(item => item.Id == grandchild.Value);
        var archivedList = await ReadList(client, true);
        archivedList.Items.Should().HaveCount(2).And.OnlyContain(item => item.DeletedAt != null);
        await ExecuteDbContextAsync(async db =>
        {
            var grandchildEntity = await db.Departments.SingleAsync(item => item.Id == grandchild);
            grandchildEntity.Path.Should().Be("deleted_archiveroot.deleted_archivechild.archivegrandchild");
            grandchildEntity.DeletedAt.Should().BeNull();
            (await db.Locations.IgnoreQueryFilters().SingleAsync(item => item.Id == location)).DeletedAt.Should().NotBeNull();
            (await db.Positions.IgnoreQueryFilters().SingleAsync(item => item.Id == position)).DeletedAt.Should().NotBeNull();
            (await db.Set<DepartmentLocation>().IgnoreQueryFilters()
                .CountAsync(item => item.LocationId == location)).Should().Be(2);
            (await db.Set<DepartmentPosition>().IgnoreQueryFilters()
                .CountAsync(item => item.PositionId == position)).Should().Be(2);
        });
        var locationsAfter = await client.GetFromJsonAsync<Envelope<PagedList<DirectoryService.Contracts.Locations.LocationListItemDto>>>(
            "/api/locations");
        locationsAfter!.Result!.Items.Should().NotContain(item => item.Id == location.Value);

        var restore = await SendBulk(client, "restore", [child.Value, root.Value]);

        restore.SucceededIds.Should().Equal(root.Value, child.Value);
        restore.Errors.Should().BeEmpty();
        (await ReadList(client)).Items.Should().HaveCount(3);
        (await ReadList(client, true)).Items.Should().BeEmpty();
        await ExecuteDbContextAsync(async db =>
        {
            (await db.Departments.SingleAsync(item => item.Id == grandchild))
                .Path.Should().Be("archiveroot.archivechild.archivegrandchild");
            (await db.Locations.IgnoreQueryFilters().SingleAsync(item => item.Id == location)).IsActive.Should().BeFalse();
            (await db.Positions.IgnoreQueryFilters().SingleAsync(item => item.Id == position)).IsActive.Should().BeFalse();
        });
    }

    [Fact]
    public async Task Archive_ShouldKeepSharedEntitiesActiveWhenUnselectedActiveDepartmentUsesThem()
    {
        var location = await SeedLocationAsync("Shared Office");
        var selected = await SeedDepartmentAsync("Selected", "selected", locationIds: [location]);
        var unselected = await SeedDepartmentAsync("Unselected", "unselected", locationIds: [location]);
        var position = await SeedPositionAsync("Shared Position", [selected, unselected]);
        using var client = CreateClient();

        var result = await SendBulk(client, "archive", [selected.Value]);

        result.Errors.Should().BeEmpty();
        await ExecuteDbContextAsync(async db =>
        {
            (await db.Locations.SingleAsync(item => item.Id == location)).IsActive.Should().BeTrue();
            (await db.Positions.SingleAsync(item => item.Id == position)).IsActive.Should().BeTrue();
        });
    }

    [Fact]
    public async Task Archive_MixedStates_ShouldArchiveValidRecordAndReportArchivedAndUnknownIds()
    {
        var archived = await SeedDepartmentAsync("Archived", "mixedarchived");
        var active = await SeedDepartmentAsync("Active", "mixedactive");
        var missing = Guid.NewGuid();
        using var client = CreateClient();
        (await client.DeleteAsync($"/api/departments/{archived.Value}")).EnsureSuccessStatusCode();

        var result = await SendBulk(client, "archive", [archived.Value, missing, active.Value]);

        result.SucceededIds.Should().ContainSingle().Which.Should().Be(active.Value);
        result.Errors.Select(error => error.Id).Should().BeEquivalentTo([archived.Value, missing]);
        (await ReadList(client)).Items.Should().BeEmpty();
        (await ReadList(client, true)).Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Restore_ShouldReportUnselectedArchivedParentWithoutBlockingOtherRecords()
    {
        var root = await SeedDepartmentAsync("Root", "restoreroot");
        var child = await SeedDepartmentAsync("Child", "restorechild", root);
        var other = await SeedDepartmentAsync("Other", "restoreother");
        using var client = CreateClient();
        foreach (var id in new[] { child, root, other })
            (await client.DeleteAsync($"/api/departments/{id.Value}")).EnsureSuccessStatusCode();

        var result = await SendBulk(client, "restore", [child.Value, other.Value]);

        result.SucceededIds.Should().ContainSingle().Which.Should().Be(other.Value);
        result.Errors.Should().ContainSingle(error =>
            error.Id == child.Value && error.Code == "department.restore.parent_deleted");
    }

    [Fact]
    public async Task Restore_AlreadyNonArchived_ShouldBeAnItemError()
    {
        var id = await SeedDepartmentAsync("Active", "active");
        using var client = CreateClient();

        var result = await SendBulk(client, "restore", [id.Value]);

        result.SucceededIds.Should().BeEmpty();
        result.Errors.Should().ContainSingle(error => error.Id == id.Value && error.Code == "department.not_found");
    }

    [Fact]
    public async Task Restore_ConstraintFailure_ShouldRollbackOnlyFailedItemAndContinue()
    {
        var failedId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var succeededId = Guid.Parse("20000000-0000-0000-0000-000000000002");
        await ExecuteDbContextAsync(async db =>
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO departments (id, name, slug, path, depth, is_active, deleted_at, created_at, updated_at)
                VALUES ({failedId}, 'Conflicting', 'conflicting', 'deleted_conflicting'::ltree, 0, FALSE, NOW(), NOW(), NOW()),
                       ({succeededId}, 'Restorable', 'restorable', 'deleted_restorable'::ltree, 0, FALSE, NOW(), NOW(), NOW()),
                       ({Guid.NewGuid()}, 'Occupied', 'occupied', 'conflicting'::ltree, 0, TRUE, NULL, NOW(), NOW())
                """);
        });
        using var client = CreateClient();

        var result = await SendBulk(client, "restore", [succeededId, failedId]);

        result.SucceededIds.Should().ContainSingle().Which.Should().Be(succeededId);
        result.Errors.Should().ContainSingle(error => error.Id == failedId);
        await ExecuteDbContextAsync(async db =>
        {
            var failedDepartmentId = DepartmentId.Create(failedId);
            var succeededDepartmentId = DepartmentId.Create(succeededId);
            var failed = await db.Departments.IgnoreQueryFilters().SingleAsync(item => item.Id == failedDepartmentId);
            failed.Path.Should().Be("deleted_conflicting");
            failed.IsActive.Should().BeFalse();
            failed.DeletedAt.Should().NotBeNull();
            var succeeded = await db.Departments.SingleAsync(item => item.Id == succeededDepartmentId);
            succeeded.Path.Should().Be("restorable");
            succeeded.DeletedAt.Should().BeNull();
        });
    }

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task InvalidBody_ShouldReturnValidationEnvelope(string action, string json)
    {
        using var client = CreateClient();
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = action == "activity"
            ? await client.PatchAsync("/api/departments/bulk/activity", content)
            : await client.PostAsync($"/api/departments/bulk/{action}", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<Envelope>())!.IsError.Should().BeTrue();
    }

    [Theory]
    [InlineData("archive")]
    [InlineData("restore")]
    [InlineData("activity")]
    public async Task DuplicateOrOversizedIds_ShouldRejectEntireRequestWithoutMutations(string action)
    {
        var id = await SeedDepartmentAsync("Unchanged", "unchanged");
        using var client = CreateClient();
        foreach (var ids in new[]
                 {
                     new[] { id.Value, id.Value },
                     new[] { id.Value }.Concat(Enumerable.Range(0, 100).Select(_ => Guid.NewGuid())).ToArray(),
                     new[] { id.Value, Guid.Empty },
                 })
        {
            var response = action == "activity"
                ? await client.PatchAsJsonAsync("/api/departments/bulk/activity", new { ids, isActive = false })
                : await client.PostAsJsonAsync($"/api/departments/bulk/{action}", new { ids });
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        await ExecuteDbContextAsync(async db =>
        {
            var department = await db.Departments.SingleAsync(item => item.Id == id);
            department.IsActive.Should().BeTrue();
            department.DeletedAt.Should().BeNull();
            department.Path.Should().Be("unchanged");
        });
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"isActive\":null}")]
    public async Task Activity_MissingTarget_ShouldNotDefaultToFalse(string target)
    {
        var id = await SeedDepartmentAsync("Unchanged", "unchanged");
        var json = target == "{}"
            ? $"{{\"ids\":[\"{id.Value}\"]}}"
            : $"{{\"ids\":[\"{id.Value}\"],\"isActive\":null}}";
        using var client = CreateClient();
        var response = await client.PatchAsync(
            "/api/departments/bulk/activity",
            new StringContent(json, Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ReadList(client)).Items.Should().ContainSingle(item => item.IsActive);
    }

    [Fact]
    public async Task Limit_ShouldAllowExactlyOneHundredIds()
    {
        Guid[] ids = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToArray();
        using var client = CreateClient();

        var result = await SendBulk(client, "activity", ids, false);

        result.SucceededCount.Should().Be(0);
        result.FailedCount.Should().Be(100);
    }

    [Fact]
    public async Task Cancellation_AfterFirstCommit_ShouldStopProcessingAndInvalidateEarlierSuccess()
    {
        var first = await SeedDepartmentAsync("First", "cancelfirst");
        var second = await SeedDepartmentAsync("Second", "cancelsecond");
        using var client = CreateClient();
        (await ReadList(client)).Items.Should().OnlyContain(item => item.IsActive);
        using var cancellation = new CancellationTokenSource();

        await ExecuteScopedAsync(async services =>
        {
            var executor = new CancellingExecutor(
                services.GetRequiredService<IDepartmentBulkItemExecutor>(), cancellation);
            var handler = new BulkDepartmentHandler(
                new BulkDepartmentCommandValidator(),
                services.GetRequiredService<IReadDbContext>(),
                executor,
                services.GetRequiredService<ICacheInvalidator>(),
                NullLogger<BulkDepartmentHandler>.Instance);
            Func<Task> action = async () => await handler.Handle(
                new BulkDepartmentCommand([first.Value, second.Value], DepartmentBulkAction.Activity, false),
                cancellation.Token);

            await action.Should().ThrowAsync<OperationCanceledException>();
            executor.Calls.Should().Be(1);
        });

        var list = await ReadList(client);
        list.Items.Count(item => item.IsActive).Should().Be(1);
        list.Items.Count(item => !item.IsActive).Should().Be(1);
    }

    private static async Task<BulkDepartmentResultDto> SendBulk(
        HttpClient client, string action, Guid[] ids, bool? isActive = null)
    {
        var response = action == "activity"
            ? await client.PatchAsJsonAsync("/api/departments/bulk/activity", new { ids, isActive })
            : await client.PostAsJsonAsync($"/api/departments/bulk/{action}", new { ids });
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<BulkDepartmentResultDto>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope!.IsError.Should().BeFalse();
        envelope.Errors.Should().BeEmpty();
        envelope.Result.Should().NotBeNull();
        return envelope.Result!;
    }

    private static async Task<PagedList<DepartmentListItemDto>> ReadList(HttpClient client, bool archived = false) =>
        (await client.GetFromJsonAsync<Envelope<PagedList<DepartmentListItemDto>>>(
            $"/api/departments?isArchived={archived.ToString().ToLowerInvariant()}&pageSize=100"))!.Result!;

    private sealed class CancellingExecutor(
        IDepartmentBulkItemExecutor inner,
        CancellationTokenSource cancellation) : IDepartmentBulkItemExecutor
    {
        public int Calls { get; private set; }

        public async Task<UnitResult<Error>> ExecuteAsync(
            Guid id, DepartmentBulkAction action, bool? isActive, CancellationToken cancellationToken)
        {
            Calls++;
            var result = await inner.ExecuteAsync(id, action, isActive, cancellationToken);
            cancellation.Cancel();
            return result;
        }
    }
}
