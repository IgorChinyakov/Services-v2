using DirectoryService.Application.Features.Departments.SoftDelete;
using DirectoryService.Domain.Shared;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public class SoftDeleteDepartmentTests : DirectoryServiceTestsBase
{
    public SoftDeleteDepartmentTests(DirectoryServiceWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task SoftDelete_WithDepartmentTreeAndRelations_ShouldApplyAllSoftDeleteRules()
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
        var childId = await SeedDepartmentAsync(
            "Regional Sales",
            "regional",
            departmentId);
        var grandchildId = await SeedDepartmentAsync(
            "Inside Sales",
            "inside",
            childId);
        var sharedPositionId = await SeedPositionAsync(
            "Account Manager",
            [departmentId, otherDepartmentId]);
        var orphanPositionId = await SeedPositionAsync(
            "Sales Representative",
            [departmentId]);
        var command = new SoftDeleteDepartmentCommand(departmentId.Value);
        var startedAt = DateTime.UtcNow;

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.GetMessage() : string.Empty);

        await ExecuteDbContextAsync(async dbContext =>
        {
            var department = await dbContext.Departments
                .AsNoTracking()
                .Include(item => item.DepartmentLocations)
                .Include(item => item.DepartmentPositions)
                .SingleAsync(item => item.Id == departmentId);
            var child = await dbContext.Departments
                .AsNoTracking()
                .SingleAsync(item => item.Id == childId);
            var grandchild = await dbContext.Departments
                .AsNoTracking()
                .SingleAsync(item => item.Id == grandchildId);
            var locations = await dbContext.Locations
                .AsNoTracking()
                .Where(item => item.Id == sharedLocationId || item.Id == orphanLocationId)
                .ToDictionaryAsync(item => item.Id);
            var positions = await dbContext.Positions
                .AsNoTracking()
                .Where(item => item.Id == sharedPositionId || item.Id == orphanPositionId)
                .ToDictionaryAsync(item => item.Id);

            department.IsActive.Should().BeFalse();
            department.DeletedAt.Should().NotBeNull();
            department.DeletedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
            department.DeletedAt.Value.Should().BeOnOrAfter(startedAt);
            department.UpdatedAt.Should().BeOnOrAfter(department.DeletedAt.Value);
            department.Path.Should().Be("deleted_sales");
            department.Depth.Should().Be(0);
            department.DepartmentLocations.Select(link => link.LocationId)
                .Should().BeEquivalentTo([sharedLocationId, orphanLocationId]);
            department.DepartmentPositions.Select(link => link.PositionId)
                .Should().BeEquivalentTo([sharedPositionId, orphanPositionId]);

            child.IsActive.Should().BeTrue();
            child.Path.Should().Be("deleted_sales.regional");
            child.Depth.Should().Be(1);
            grandchild.IsActive.Should().BeTrue();
            grandchild.Path.Should().Be("deleted_sales.regional.inside");
            grandchild.Depth.Should().Be(2);

            locations[sharedLocationId].IsActive.Should().BeTrue();
            locations[orphanLocationId].IsActive.Should().BeFalse();
            positions[sharedPositionId].IsActive.Should().BeTrue();
            positions[orphanPositionId].IsActive.Should().BeFalse();
        });
    }

    [Fact]
    public async Task SoftDelete_WithMissingDepartment_ShouldReturnNotFoundError()
    {
        // Arrange
        var command = new SoftDeleteDepartmentCommand(Guid.NewGuid());

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound, result.Error.GetMessage());
    }

    [Fact]
    public async Task SoftDelete_WithInactiveDepartment_ShouldReturnNotFoundError()
    {
        // Arrange
        var departmentId = await SeedDepartmentAsync("Sales Department", "sales");
        var command = new SoftDeleteDepartmentCommand(departmentId.Value);
        var firstResult = await ExecuteCommandAsync(command);
        firstResult.IsSuccess.Should().BeTrue(
            firstResult.IsFailure ? firstResult.Error.GetMessage() : string.Empty);

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound, result.Error.GetMessage());
    }

    [Fact]
    public async Task SoftDelete_WithEmptyDepartmentId_ShouldReturnValidationError()
    {
        // Arrange
        var command = new SoftDeleteDepartmentCommand(Guid.Empty);

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation, result.Error.GetMessage());
        result.Error.Messages.Should().ContainSingle(message =>
            message.InvalidField == nameof(SoftDeleteDepartmentCommand.DepartmentId));
    }

    [Fact]
    public async Task SoftDelete_WhenSavingDepartmentFails_ShouldRollbackAllChanges()
    {
        // Arrange
        var orphanLocationId = await SeedLocationAsync("Sales Office");
        var departmentId = await SeedDepartmentAsync(
            "Sales Department",
            "sales",
            locationIds: [orphanLocationId]);
        var childId = await SeedDepartmentAsync(
            "Regional Sales",
            "regional",
            departmentId);
        var orphanPositionId = await SeedPositionAsync(
            "Sales Representative",
            [departmentId]);
        var command = new SoftDeleteDepartmentCommand(departmentId.Value);

        await ExecuteDbContextAsync(dbContext =>
            dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 INSERT INTO departments (
                     id,
                     name,
                     identifier,
                     parent_id,
                     path,
                     depth,
                     is_active,
                     created_at,
                     updated_at)
                 VALUES (
                     {Guid.NewGuid()},
                     'Path Conflict',
                     'pathconflict',
                     NULL,
                     'deleted_sales'::ltree,
                     0,
                     TRUE,
                     NOW(),
                     NOW())
                 """));

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict, result.Error.GetMessage());

        await ExecuteDbContextAsync(async dbContext =>
        {
            var department = await dbContext.Departments
                .AsNoTracking()
                .SingleAsync(item => item.Id == departmentId);
            var child = await dbContext.Departments
                .AsNoTracking()
                .SingleAsync(item => item.Id == childId);
            var location = await dbContext.Locations
                .AsNoTracking()
                .SingleAsync(item => item.Id == orphanLocationId);
            var position = await dbContext.Positions
                .AsNoTracking()
                .SingleAsync(item => item.Id == orphanPositionId);

            department.IsActive.Should().BeTrue();
            department.DeletedAt.Should().BeNull();
            department.Path.Should().Be("sales");
            child.IsActive.Should().BeTrue();
            child.Path.Should().Be("sales.regional");
            location.IsActive.Should().BeTrue();
            position.IsActive.Should().BeTrue();
        });
    }
}
