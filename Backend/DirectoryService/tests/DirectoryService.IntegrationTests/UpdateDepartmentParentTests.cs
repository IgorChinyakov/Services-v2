using System.Net;
using System.Net.Http.Json;
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
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope!.IsError.Should().BeFalse();
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

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
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
        envelope!.Errors.Should().ContainSingle(error => error.Code == "department.parent.descendant");
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

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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
}
