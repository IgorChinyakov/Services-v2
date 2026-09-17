using System.Net;
using System.Net.Http.Json;
using DirectoryService.Contracts.Departments;
using DirectoryService.Contracts.Departments.Requests;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Presentation.ApiResponse;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public sealed class UpdateDepartmentParentTests : DirectoryServiceTestsBase
{
    public UpdateDepartmentParentTests(DirectoryServiceWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task UpdateDepartmentParent_WithValidParent_ShouldMoveEntireSubtree()
    {
        // Arrange
        var hqId = await SeedDepartmentAsync("Head Office", "head");
        var salesId = await SeedDepartmentAsync("Sales Department", "sales", hqId);
        var regionalId = await SeedDepartmentAsync("Regional Department", "regional", salesId);
        var southId = await SeedDepartmentAsync("South Department", "south", regionalId);
        using var client = CreateClient();

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/departments/{regionalId.Value}/parent",
            new UpdateDepartmentParentRequest { ParentId = hqId.Value });
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<DepartmentMoveResultDto>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope!.IsError.Should().BeFalse();
        envelope.Result.Should().NotBeNull();
        envelope.Result!.Id.Should().Be(regionalId.Value);
        envelope.Result.ParentId.Should().Be(hqId.Value);
        envelope.Result.Path.Should().Be("head.regional");
        envelope.Result.Depth.Should().Be(1);
        envelope.Result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        await AssertDepartmentAsync(regionalId, hqId, "head.regional", 1);
        await AssertDepartmentAsync(southId, regionalId, "head.regional.south", 2);
    }

    [Fact]
    public async Task UpdateDepartmentParent_WithNullParent_ShouldMoveEntireSubtreeToRoot()
    {
        // Arrange
        var hqId = await SeedDepartmentAsync("Head Office", "head");
        var regionalId = await SeedDepartmentAsync("Regional Department", "regional", hqId);
        var southId = await SeedDepartmentAsync("South Department", "south", regionalId);
        using var client = CreateClient();

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/departments/{regionalId.Value}/parent",
            new UpdateDepartmentParentRequest { ParentId = null });
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<DepartmentMoveResultDto>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope!.Result.Should().NotBeNull();
        envelope.Result!.Id.Should().Be(regionalId.Value);
        envelope.Result.ParentId.Should().BeNull();
        envelope.Result.Path.Should().Be("regional");
        envelope.Result.Depth.Should().Be(0);
        await AssertDepartmentAsync(regionalId, null, "regional", 0);
        await AssertDepartmentAsync(southId, regionalId, "regional.south", 1);
    }

    [Fact]
    public async Task UpdateDepartmentParent_WithDescendantParent_ShouldReturnConflictAndKeepTree()
    {
        // Arrange
        var hqId = await SeedDepartmentAsync("Head Office", "head");
        var salesId = await SeedDepartmentAsync("Sales Department", "sales", hqId);
        var regionalId = await SeedDepartmentAsync("Regional Department", "regional", salesId);
        var southId = await SeedDepartmentAsync("South Department", "south", regionalId);
        using var client = CreateClient();

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/departments/{salesId.Value}/parent",
            new UpdateDepartmentParentRequest { ParentId = southId.Value });
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        envelope!.Errors.Should().ContainSingle(error => error.Code == "department.move.cycle");
        await AssertDepartmentAsync(salesId, hqId, "head.sales", 1);
        await AssertDepartmentAsync(regionalId, salesId, "head.sales.regional", 2);
        await AssertDepartmentAsync(southId, regionalId, "head.sales.regional.south", 3);
    }

    [Fact]
    public async Task UpdateDepartmentParent_WithMissingParent_ShouldReturnNotFound()
    {
        // Arrange
        var departmentId = await SeedDepartmentAsync("Sales Department", "sales");
        using var client = CreateClient();

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/departments/{departmentId.Value}/parent",
            new UpdateDepartmentParentRequest { ParentId = Guid.NewGuid() });
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        envelope!.Errors.Should().ContainSingle(error => error.Code == "department.parent.not_found");
    }

    [Fact]
    public async Task UpdateDepartmentParent_WithSameIds_ShouldReturnBadRequest()
    {
        // Arrange
        var departmentId = DepartmentId.New();
        using var client = CreateClient();

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/departments/{departmentId.Value}/parent",
            new UpdateDepartmentParentRequest { ParentId = departmentId.Value });
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        envelope!.Errors.Should().ContainSingle(error => error.Code == "department.move.parent_is_self");
    }

    [Fact]
    public async Task UpdateDepartmentParent_WhenDepartmentDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var missingDepartmentId = Guid.NewGuid();
        using var client = CreateClient();

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/departments/{missingDepartmentId}/parent",
            new UpdateDepartmentParentRequest { ParentId = null });
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        envelope!.Errors.Should().ContainSingle(error => error.Code == "department.not_found");
    }

    [Fact]
    public async Task UpdateDepartmentParent_WithSoftDeletedParent_ShouldReturnConflict()
    {
        // Arrange
        var deletedParentId = await SeedDepartmentAsync("Deleted Parent", "deletedparent");
        var departmentId = await SeedDepartmentAsync("Sales Department", "sales");
        await ExecuteDbContextAsync(async dbContext =>
        {
            var deletedParent = await dbContext.Departments
                .SingleAsync(department => department.Id == deletedParentId);

            deletedParent.SoftDelete();
            await dbContext.SaveChangesAsync();
        });
        using var client = CreateClient();

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/departments/{departmentId.Value}/parent",
            new UpdateDepartmentParentRequest { ParentId = deletedParentId.Value });
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        envelope!.Errors.Should().ContainSingle(error => error.Code == "department.move.parent_deleted");
        await AssertDepartmentAsync(departmentId, null, "sales", 0);
    }

    [Fact]
    public async Task UpdateDepartmentParent_WithCurrentParent_ShouldBeNoOp()
    {
        // Arrange
        var parentId = await SeedDepartmentAsync("Head Office", "head");
        var departmentId = await SeedDepartmentAsync("Sales Department", "sales", parentId);
        var updatedAtBeforeMove = await GetDepartmentUpdatedAtAsync(departmentId);
        ResetDepartmentUpdateCount();
        using var client = CreateClient();

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/departments/{departmentId.Value}/parent",
            new UpdateDepartmentParentRequest { ParentId = parentId.Value });
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<DepartmentMoveResultDto>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope!.Result.Should().NotBeNull();
        envelope.Result!.Id.Should().Be(departmentId.Value);
        envelope.Result.ParentId.Should().Be(parentId.Value);
        envelope.Result.Path.Should().Be("head.sales");
        envelope.Result.Depth.Should().Be(1);
        envelope.Result.UpdatedAt.Should().Be(updatedAtBeforeMove);
        GetDepartmentUpdateCount().Should().Be(0);
        (await GetDepartmentUpdatedAtAsync(departmentId)).Should().Be(updatedAtBeforeMove);
    }

    [Fact]
    public async Task UpdateDepartmentParent_WithLargeSubtree_ShouldUseSingleBulkUpdate()
    {
        // Arrange
        var locationId = await SeedLocationAsync();
        var originalParentId = await SeedDepartmentAsync(
            "Original Parent",
            "originalparent",
            locationIds: [locationId]);
        var newParentId = await SeedDepartmentAsync(
            "New Parent",
            "newparent",
            locationIds: [locationId]);
        var movingDepartmentId = await SeedDepartmentAsync(
            "Moving Department",
            "movingdepartment",
            originalParentId,
            [locationId]);
        var deepestDepartmentId = movingDepartmentId;
        var deepestParentId = originalParentId;
        var descendantSlugs = new List<string>();

        for (var index = 0; index < 20; index++)
        {
            var suffix = ((char)('a' + index)).ToString();
            var slug = $"node{suffix}";
            descendantSlugs.Add(slug);
            deepestParentId = deepestDepartmentId;
            deepestDepartmentId = await SeedDepartmentAsync(
                $"Node {suffix.ToUpperInvariant()}",
                slug,
                deepestDepartmentId,
                [locationId]);
        }

        ResetDepartmentUpdateCount();
        using var client = CreateClient();

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/departments/{movingDepartmentId.Value}/parent",
            new UpdateDepartmentParentRequest { ParentId = newParentId.Value });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        GetDepartmentUpdateCount().Should().Be(1);
        await AssertDepartmentAsync(
            deepestDepartmentId,
            deepestParentId,
            $"newparent.movingdepartment.{string.Join('.', descendantSlugs)}",
            (short)(descendantSlugs.Count + 1));
    }

    private Task AssertDepartmentAsync(
        DepartmentId departmentId,
        DepartmentId? parentId,
        string path,
        short depth)
    {
        return ExecuteDbContextAsync(async dbContext =>
        {
            var department = await dbContext.Departments
                .AsNoTracking()
                .FirstAsync(item => item.Id == departmentId);

            department.ParentId.Should().Be(parentId);
            department.Path.Should().Be(path);
            department.Depth.Should().Be(depth);
        });
    }

    private Task<DateTime> GetDepartmentUpdatedAtAsync(DepartmentId departmentId)
    {
        return ExecuteDbContextAsync(async dbContext =>
        {
            return await dbContext.Departments
                .AsNoTracking()
                .Where(department => department.Id == departmentId)
                .Select(department => department.UpdatedAt)
                .SingleAsync();
        });
    }

}
