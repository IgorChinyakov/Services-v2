using DirectoryService.Application.Features.Departments.UpdateLocations;
using DirectoryService.Domain.Shared;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public class UpdateDepartmentLocationsTests : DirectoryServiceTestsBase
{
    public UpdateDepartmentLocationsTests(DirectoryServiceWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task UpdateDepartmentLocations_WithValidData_ShouldReplaceLocations()
    {
        // Arrange
        var oldLocationId = await SeedLocationAsync();
        var newLocationId = await SeedLocationAsync();
        var departmentId = await SeedDepartmentAsync(
            "Sales Department",
            "sales",
            locationIds: [oldLocationId]);
        var command = new UpdateDepartmentLocationsCommand(
            departmentId.Value,
            [newLocationId.Value]);

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.GetMessage() : string.Empty);

        await ExecuteDbContextAsync(async dbContext =>
        {
            var department = await dbContext.Departments
                .Include(x => x.DepartmentLocations)
                .FirstAsync(x => x.Id == departmentId);

            department.DepartmentLocations
                .Select(link => link.LocationId)
                .Should()
                .BeEquivalentTo([newLocationId]);
        });
    }

    [Fact]
    public async Task UpdateDepartmentLocations_WithMissingDepartment_ShouldReturnNotFoundError()
    {
        // Arrange
        var locationId = await SeedLocationAsync();
        var command = new UpdateDepartmentLocationsCommand(
            Guid.NewGuid(),
            [locationId.Value]);

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound, result.Error.GetMessage());
    }

    [Fact]
    public async Task UpdateDepartmentLocations_WithMissingLocation_ShouldReturnValidationErrorAndKeepOldLocations()
    {
        // Arrange
        var oldLocationId = await SeedLocationAsync();
        var departmentId = await SeedDepartmentAsync(
            "Sales Department",
            "sales",
            locationIds: [oldLocationId]);
        var command = new UpdateDepartmentLocationsCommand(
            departmentId.Value,
            [Guid.NewGuid()]);

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation, result.Error.GetMessage());
        result.Error.Messages.Should().ContainSingle(message =>
            message.InvalidField == nameof(UpdateDepartmentLocationsCommand.LocationIds));

        await ExecuteDbContextAsync(async dbContext =>
        {
            var department = await dbContext.Departments
                .Include(x => x.DepartmentLocations)
                .FirstAsync(x => x.Id == departmentId);

            department.DepartmentLocations
                .Select(link => link.LocationId)
                .Should()
                .BeEquivalentTo([oldLocationId]);
        });
    }

    [Fact]
    public async Task UpdateDepartmentLocations_WithDuplicateLocationIds_ShouldReturnValidationError()
    {
        // Arrange
        var locationId = await SeedLocationAsync();
        var departmentId = await SeedDepartmentAsync(
            "Sales Department",
            "sales",
            locationIds: [locationId]);
        var command = new UpdateDepartmentLocationsCommand(
            departmentId.Value,
            [locationId.Value, locationId.Value]);

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation, result.Error.GetMessage());
        result.Error.Messages.Should().ContainSingle(message =>
            message.InvalidField == nameof(UpdateDepartmentLocationsCommand.LocationIds));
    }
}
