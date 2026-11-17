using System.Net;
using System.Net.Http.Json;
using DirectoryService.Contracts.Common;
using DirectoryService.Contracts.Departments;
using DirectoryService.Domain.Entities;
using DirectoryService.Presentation.ApiResponse;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public sealed class RestoreDepartmentTests : DirectoryServiceTestsBase
{
    public RestoreDepartmentTests(DirectoryServiceWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Restore_ShouldRepairSubtreeAndInvalidateListsWithoutRestoringOtherArchivedEntities()
    {
        var locationId = await SeedLocationAsync("Restore Department Office");
        var rootId = await SeedDepartmentAsync("Restore Root", "restoreroot", locationIds: [locationId]);
        var childId = await SeedDepartmentAsync("Restore Child", "restorechild", rootId);
        var archivedChildId = await SeedDepartmentAsync("Archived Child", "archivedchild", rootId);
        var positionId = await SeedPositionAsync("Restore Position", [rootId]);
        using var client = CreateClient();

        (await client.DeleteAsync($"/api/departments/{archivedChildId.Value}"))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.DeleteAsync($"/api/departments/{rootId.Value}"))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var archivedBefore = await client.GetFromJsonAsync<Envelope<PagedList<DepartmentListItemDto>>>(
            "/api/departments?isArchived=true");
        archivedBefore!.Result!.Items.Should().HaveCount(2);
        archivedBefore.Result.Items.Should().OnlyContain(item => !item.IsActive && item.DeletedAt != null);
        var activeBefore = await client.GetFromJsonAsync<Envelope<PagedList<DepartmentListItemDto>>>(
            "/api/departments?isArchived=false");
        activeBefore!.Result!.Items.Should().NotContain(item => item.Id == rootId.Value);

        var response = await client.PatchAsync($"/api/departments/{rootId.Value}/restore", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<Envelope>())!.IsError.Should().BeFalse();

        await ExecuteDbContextAsync(async dbContext =>
        {
            var root = await dbContext.Departments.SingleAsync(item => item.Id == rootId);
            var child = await dbContext.Departments.SingleAsync(item => item.Id == childId);
            var archivedChild = await dbContext.Departments.IgnoreQueryFilters()
                .SingleAsync(item => item.Id == archivedChildId);
            root.Path.Should().Be("restoreroot");
            root.IsActive.Should().BeTrue();
            root.DeletedAt.Should().BeNull();
            child.Path.Should().Be("restoreroot.restorechild");
            child.ParentId.Should().Be(rootId);
            archivedChild.Path.Should().Be("restoreroot.deleted_archivedchild");
            archivedChild.IsActive.Should().BeFalse();
            archivedChild.DeletedAt.Should().NotBeNull();
            (await dbContext.Set<DepartmentLocation>().IgnoreQueryFilters()
                .AnyAsync(link => link.DepartmentId == rootId && link.LocationId == locationId)).Should().BeTrue();
            (await dbContext.Locations.IgnoreQueryFilters().SingleAsync(item => item.Id == locationId))
                .IsActive.Should().BeFalse();
            (await dbContext.Positions.IgnoreQueryFilters().SingleAsync(item => item.Id == positionId))
                .IsActive.Should().BeFalse();
        });

        var activeAfter = await client.GetFromJsonAsync<Envelope<PagedList<DepartmentListItemDto>>>(
            "/api/departments?isArchived=false");
        var archivedAfter = await client.GetFromJsonAsync<Envelope<PagedList<DepartmentListItemDto>>>(
            "/api/departments?isArchived=true");
        activeAfter!.Result!.Items.Should().Contain(item => item.Id == rootId.Value && item.DeletedAt == null);
        archivedAfter!.Result!.Items.Should().ContainSingle(item => item.Id == archivedChildId.Value);
    }

    [Fact]
    public async Task Restore_WithArchivedParent_ShouldRequireParentFirst()
    {
        var rootId = await SeedDepartmentAsync("Parent First", "parentfirst");
        var childId = await SeedDepartmentAsync("Child Later", "childlater", rootId);
        using var client = CreateClient();
        (await client.DeleteAsync($"/api/departments/{childId.Value}")).EnsureSuccessStatusCode();
        (await client.DeleteAsync($"/api/departments/{rootId.Value}")).EnsureSuccessStatusCode();

        var response = await client.PatchAsync($"/api/departments/{childId.Value}/restore", null);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();
        envelope!.Errors.Should().ContainSingle(error => error.Code == "department.restore.parent_deleted");

        (await client.PatchAsync($"/api/departments/{rootId.Value}/restore", null)).EnsureSuccessStatusCode();
        (await client.PatchAsync($"/api/departments/{childId.Value}/restore", null)).EnsureSuccessStatusCode();
        await ExecuteDbContextAsync(async dbContext =>
        {
            var child = await dbContext.Departments.SingleAsync(item => item.Id == childId);
            child.Path.Should().Be("parentfirst.childlater");
            child.Depth.Should().Be(1);
            child.DeletedAt.Should().BeNull();
        });
    }

    [Fact]
    public async Task Restore_WithPathConflict_ShouldRollbackSubtree()
    {
        var rootId = await SeedDepartmentAsync("Conflicting Root", "conflictingroot");
        var childId = await SeedDepartmentAsync("Conflicting Child", "conflictingchild", rootId);
        using var client = CreateClient();
        (await client.DeleteAsync($"/api/departments/{rootId.Value}")).EnsureSuccessStatusCode();
        await ExecuteDbContextAsync(dbContext => dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO departments (id, name, slug, parent_id, path, depth, is_active, created_at, updated_at)
             VALUES ({Guid.NewGuid()}, 'Occupied Path', 'occupiedslug', NULL,
                     'conflictingroot'::ltree, 0, TRUE, NOW(), NOW())
             """));

        var response = await client.PatchAsync($"/api/departments/{rootId.Value}/restore", null);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await ExecuteDbContextAsync(async dbContext =>
        {
            var root = await dbContext.Departments.IgnoreQueryFilters().SingleAsync(item => item.Id == rootId);
            var child = await dbContext.Departments.SingleAsync(item => item.Id == childId);
            root.IsActive.Should().BeFalse();
            root.Path.Should().Be("deleted_conflictingroot");
            child.Path.Should().Be("deleted_conflictingroot.conflictingchild");
        });
    }

    [Fact]
    public async Task ArchiveFilter_ShouldExcludeInactiveDepartmentsWithoutDeletedAt()
    {
        var archivedId = await SeedDepartmentAsync("Archived", "archivefilter");
        var inactiveId = await SeedDepartmentAsync("Inactive", "inactivefilter");
        await ExecuteDbContextAsync(async dbContext =>
        {
            var archived = await dbContext.Departments.SingleAsync(item => item.Id == archivedId);
            archived.SoftDelete();
            await dbContext.SaveChangesAsync();
            await dbContext.Departments.Where(item => item.Id == inactiveId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.IsActive, false));
        });
        using var client = CreateClient();
        var archivedResponse = await client.GetFromJsonAsync<Envelope<PagedList<DepartmentListItemDto>>>(
            "/api/departments?isArchived=true&search=Archived&pageSize=1");
        archivedResponse!.Result!.Items.Should().ContainSingle(item => item.Id == archivedId.Value);
        (await client.PatchAsync($"/api/departments/{inactiveId.Value}/restore", null))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Restore_WithEmptyOrMissingId_ShouldReturnValidationOrNotFound()
    {
        using var client = CreateClient();
        (await client.PatchAsync($"/api/departments/{Guid.Empty}/restore", null))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.PatchAsync($"/api/departments/{Guid.NewGuid()}/restore", null))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        var activeId = await SeedDepartmentAsync("Already Active", "alreadyactive");
        (await client.PatchAsync($"/api/departments/{activeId.Value}/restore", null))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
