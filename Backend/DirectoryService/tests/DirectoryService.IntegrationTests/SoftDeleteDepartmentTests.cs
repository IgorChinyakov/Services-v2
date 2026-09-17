using System.Net;
using System.Net.Http.Json;
using DirectoryService.Presentation.ApiResponse;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public sealed class SoftDeleteDepartmentTests : DirectoryServiceTestsBase
{
    public SoftDeleteDepartmentTests(DirectoryServiceWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task SoftDelete_WithDepartmentTreeAndRelations_ShouldApplyAllRules()
    {
        // Arrange
        var sharedLocationId = await SeedLocationAsync("Shared Office");
        var orphanLocationId = await SeedLocationAsync("Sales Office");
        var departmentId = await SeedDepartmentAsync(
            "Sales Department",
            "sales",
            locationIds: [sharedLocationId, orphanLocationId]);
        var otherDepartmentId = await SeedDepartmentAsync(
            "Marketing Department",
            "marketing",
            locationIds: [sharedLocationId]);
        var childId = await SeedDepartmentAsync("Regional Sales", "regional", departmentId);
        var grandchildId = await SeedDepartmentAsync("Inside Sales", "inside", childId);
        var sharedPositionId = await SeedPositionAsync("Account Manager", [departmentId, otherDepartmentId]);
        var orphanPositionId = await SeedPositionAsync("Sales Representative", [departmentId]);
        using var client = CreateClient();
        var startedAt = DateTime.UtcNow;

        // Act
        var response = await client.DeleteAsync($"/api/departments/{departmentId.Value}");
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope!.IsError.Should().BeFalse();
        await ExecuteDbContextAsync(async dbContext =>
        {
            var department = await dbContext.Departments
                .AsNoTracking()
                .Include(item => item.DepartmentLocations)
                .Include(item => item.DepartmentPositions)
                .SingleAsync(item => item.Id == departmentId);
            var child = await dbContext.Departments.AsNoTracking().SingleAsync(item => item.Id == childId);
            var grandchild = await dbContext.Departments.AsNoTracking().SingleAsync(item => item.Id == grandchildId);
            var locations = await dbContext.Locations.AsNoTracking()
                .Where(item => item.Id == sharedLocationId || item.Id == orphanLocationId)
                .ToDictionaryAsync(item => item.Id);
            var positions = await dbContext.Positions.AsNoTracking()
                .Where(item => item.Id == sharedPositionId || item.Id == orphanPositionId)
                .ToDictionaryAsync(item => item.Id);

            department.IsActive.Should().BeFalse();
            department.DeletedAt.Should().NotBeNull();
            department.DeletedAt!.Value.Should().BeOnOrAfter(startedAt);
            department.Path.Should().Be("deleted_sales");
            department.DepartmentLocations.Should().HaveCount(2);
            department.DepartmentPositions.Should().HaveCount(2);
            child.IsActive.Should().BeTrue();
            child.Path.Should().Be("deleted_sales.regional");
            grandchild.IsActive.Should().BeTrue();
            grandchild.Path.Should().Be("deleted_sales.regional.inside");
            locations[sharedLocationId].IsActive.Should().BeTrue();
            locations[orphanLocationId].IsActive.Should().BeFalse();
            positions[sharedPositionId].IsActive.Should().BeTrue();
            positions[orphanPositionId].IsActive.Should().BeFalse();
        });
    }

    [Fact]
    public async Task SoftDelete_WithMissingDepartment_ShouldReturnNotFound()
    {
        // Arrange
        using var client = CreateClient();

        // Act
        var response = await client.DeleteAsync($"/api/departments/{Guid.NewGuid()}");
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        envelope!.Errors.Should().ContainSingle(error => error.Code == "department.not_found");
    }

    [Fact]
    public async Task SoftDelete_WithInactiveDepartment_ShouldReturnNotFoundOnSecondRequest()
    {
        // Arrange
        var departmentId = await SeedDepartmentAsync("Sales Department", "sales");
        using var client = CreateClient();
        var endpoint = $"/api/departments/{departmentId.Value}";
        var firstResponse = await client.DeleteAsync(endpoint);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var response = await client.DeleteAsync(endpoint);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        envelope!.Errors.Should().ContainSingle(error => error.Code == "department.not_found");
    }

    [Fact]
    public async Task SoftDelete_WithEmptyDepartmentId_ShouldReturnBadRequest()
    {
        // Arrange
        using var client = CreateClient();

        // Act
        var response = await client.DeleteAsync($"/api/departments/{Guid.Empty}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SoftDelete_WhenPathUpdateConflicts_ShouldRollbackAllChanges()
    {
        // Arrange
        var orphanLocationId = await SeedLocationAsync("Sales Office");
        var departmentId = await SeedDepartmentAsync(
            "Sales Department",
            "sales",
            locationIds: [orphanLocationId]);
        var childId = await SeedDepartmentAsync("Regional Sales", "regional", departmentId);
        var orphanPositionId = await SeedPositionAsync("Sales Representative", [departmentId]);
        await ExecuteDbContextAsync(dbContext =>
            dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 INSERT INTO departments (
                     id, name, slug, parent_id, path, depth, is_active, created_at, updated_at)
                 VALUES (
                     {Guid.NewGuid()}, 'Path Conflict', 'pathconflict', NULL,
                     'deleted_sales'::ltree, 0, TRUE, NOW(), NOW())
                 """));
        using var client = CreateClient();

        // Act
        var response = await client.DeleteAsync($"/api/departments/{departmentId.Value}");
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        envelope!.Errors.Should().ContainSingle(error => error.Code == "department.path.duplicate");
        await ExecuteDbContextAsync(async dbContext =>
        {
            var department = await dbContext.Departments.AsNoTracking().SingleAsync(item => item.Id == departmentId);
            var child = await dbContext.Departments.AsNoTracking().SingleAsync(item => item.Id == childId);
            var location = await dbContext.Locations.AsNoTracking().SingleAsync(item => item.Id == orphanLocationId);
            var position = await dbContext.Positions.AsNoTracking().SingleAsync(item => item.Id == orphanPositionId);

            department.IsActive.Should().BeTrue();
            department.DeletedAt.Should().BeNull();
            department.Path.Should().Be("sales");
            child.Path.Should().Be("sales.regional");
            location.IsActive.Should().BeTrue();
            position.IsActive.Should().BeTrue();
        });
    }
}
