using DirectoryService.Application.Features.Departments.Create;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.Shared;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public class CreateDepartmentTests : DirectoryServiceTestsBase
{
    public CreateDepartmentTests(DirectoryServiceWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CreateDepartment_WithValidRootData_ShouldCreateDepartment()
    {
        // Arrange
        var locationId = await SeedLocationAsync();
        var command = new CreateDepartmentCommand(
            "Sales Department",
            "sales",
            null,
            [locationId.Value]);

        // Act
        var result = await ExecuteCommandAsync<CreateDepartmentCommand, Guid>(command);

        // Assert
        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.GetMessage() : string.Empty);

        await ExecuteDbContextAsync(async dbContext =>
        {
            var department = await dbContext.Departments
                .Include(x => x.DepartmentLocations)
                .FirstAsync(x => x.Id == DepartmentId.Create(result.Value));

            department.Name.Value.Should().Be(command.Name);
            department.Identifier.Value.Should().Be(command.Identifier);
            department.ParentId.Should().BeNull();
            department.Path.Should().Be("sales");
            department.Depth.Should().Be(0);
            department.DepartmentLocations.Should().ContainSingle(link => link.LocationId == locationId);
        });
    }

    [Fact]
    public async Task CreateDepartment_WithParentId_ShouldCreateChildDepartment()
    {
        // Arrange
        var parentLocationId = await SeedLocationAsync();
        var childLocationId = await SeedLocationAsync();
        var parentId = await SeedDepartmentAsync(
            "Head Office",
            "head",
            locationIds: [parentLocationId]);
        var command = new CreateDepartmentCommand(
            "Human Resources",
            "human",
            parentId.Value,
            [childLocationId.Value]);

        // Act
        var result = await ExecuteCommandAsync<CreateDepartmentCommand, Guid>(command);

        // Assert
        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.GetMessage() : string.Empty);

        await ExecuteDbContextAsync(async dbContext =>
        {
            var department = await dbContext.Departments
                .Include(x => x.DepartmentLocations)
                .FirstAsync(x => x.Id == DepartmentId.Create(result.Value));

            department.ParentId.Should().Be(parentId);
            department.Path.Should().Be("head.human");
            department.Depth.Should().Be(1);
            department.DepartmentLocations.Should().ContainSingle(link => link.LocationId == childLocationId);
        });
    }

    [Fact]
    public async Task CreateDepartment_WithMissingLocation_ShouldReturnValidationError()
    {
        // Arrange
        var missingLocationId = Guid.NewGuid();
        var command = new CreateDepartmentCommand(
            "Sales Department",
            "sales",
            null,
            [missingLocationId]);

        // Act
        var result = await ExecuteCommandAsync<CreateDepartmentCommand, Guid>(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation, result.Error.GetMessage());
        result.Error.Messages.Should().ContainSingle(message =>
            message.InvalidField == nameof(CreateDepartmentCommand.LocationIds));
    }

    [Fact]
    public async Task CreateDepartment_WithDuplicateIdentifier_ShouldReturnConflictError()
    {
        // Arrange
        var firstLocationId = await SeedLocationAsync();
        var secondLocationId = await SeedLocationAsync();
        await SeedDepartmentAsync(
            "Sales Department",
            "sales",
            locationIds: [firstLocationId]);
        var command = new CreateDepartmentCommand(
            "Another Sales Department",
            "sales",
            null,
            [secondLocationId.Value]);

        // Act
        var result = await ExecuteCommandAsync<CreateDepartmentCommand, Guid>(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict, result.Error.GetMessage());
        result.Error.Messages.Should().ContainSingle(message =>
            message.InvalidField == nameof(CreateDepartmentCommand.Identifier));
    }

    [Fact]
    public async Task CreateDepartment_WithDuplicateLocationIds_ShouldReturnValidationError()
    {
        // Arrange
        var locationId = await SeedLocationAsync();
        var command = new CreateDepartmentCommand(
            "Sales Department",
            "sales",
            null,
            [locationId.Value, locationId.Value]);

        // Act
        var result = await ExecuteCommandAsync<CreateDepartmentCommand, Guid>(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation, result.Error.GetMessage());
        result.Error.Messages.Should().ContainSingle(message =>
            message.InvalidField == nameof(CreateDepartmentCommand.LocationIds));
    }
}
