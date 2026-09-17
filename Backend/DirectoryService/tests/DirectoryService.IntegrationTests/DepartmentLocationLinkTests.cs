using DirectoryService.Application.Features.Departments.AddLocation;
using DirectoryService.Application.Features.Departments.RemoveLocation;
using DirectoryService.Domain.Entities;
using DirectoryService.Domain.Shared;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public sealed class DepartmentLocationLinkTests : DirectoryServiceTestsBase
{
    public DepartmentLocationLinkTests(DirectoryServiceWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task AddDepartmentLocation_WhenEntitiesExist_ShouldCreateLink()
    {
        // Arrange
        var departmentId = await SeedDepartmentAsync("Sales Department", "sales", locationIds: []);
        var locationId = await SeedLocationAsync();
        var command = new AddDepartmentLocationCommand(departmentId.Value, locationId.Value);

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.GetMessage() : string.Empty);
        await ExecuteDbContextAsync(async dbContext =>
        {
            var linkExists = await dbContext.Set<DepartmentLocation>()
                .AnyAsync(link => link.DepartmentId == departmentId && link.LocationId == locationId);

            linkExists.Should().BeTrue();
        });
    }

    [Fact]
    public async Task AddDepartmentLocation_WhenLinkAlreadyExists_ShouldReturnConflictWithoutDuplicate()
    {
        // Arrange
        var locationId = await SeedLocationAsync();
        var departmentId = await SeedDepartmentAsync(
            "Sales Department",
            "sales",
            locationIds: [locationId]);
        var command = new AddDepartmentLocationCommand(departmentId.Value, locationId.Value);

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Messages.Should().ContainSingle(message => message.Code == "department.location.duplicate");
        await ExecuteDbContextAsync(async dbContext =>
        {
            var linksCount = await dbContext.Set<DepartmentLocation>()
                .CountAsync(link => link.DepartmentId == departmentId && link.LocationId == locationId);

            linksCount.Should().Be(1);
        });
    }

    [Fact]
    public async Task AddDepartmentLocation_WhenLocationDoesNotExist_ShouldReturnLocationNotFound()
    {
        // Arrange
        var departmentId = await SeedDepartmentAsync("Sales Department", "sales", locationIds: []);
        var command = new AddDepartmentLocationCommand(departmentId.Value, Guid.NewGuid());

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Messages.Should().ContainSingle(message => message.Code == "location.not_found");
    }

    [Fact]
    public async Task AddDepartmentLocation_WhenDepartmentDoesNotExist_ShouldReturnDepartmentNotFound()
    {
        // Arrange
        var locationId = await SeedLocationAsync();
        var command = new AddDepartmentLocationCommand(Guid.NewGuid(), locationId.Value);

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Messages.Should().ContainSingle(message => message.Code == "department.not_found");
    }

    [Fact]
    public async Task RemoveDepartmentLocation_WhenLinkExists_ShouldDeleteOnlyLink()
    {
        // Arrange
        var locationId = await SeedLocationAsync();
        var departmentId = await SeedDepartmentAsync(
            "Sales Department",
            "sales",
            locationIds: [locationId]);
        var command = new RemoveDepartmentLocationCommand(departmentId.Value, locationId.Value);

        // Act
        var firstResult = await ExecuteCommandAsync(command);
        var secondResult = await ExecuteCommandAsync(command);

        // Assert
        firstResult.IsSuccess.Should().BeTrue(firstResult.IsFailure ? firstResult.Error.GetMessage() : string.Empty);
        secondResult.IsFailure.Should().BeTrue();
        secondResult.Error.Type.Should().Be(ErrorType.NotFound);
        secondResult.Error.Messages.Should().ContainSingle(message =>
            message.Code == "department.location.not_found");
        await ExecuteDbContextAsync(async dbContext =>
        {
            var linkExists = await dbContext.Set<DepartmentLocation>()
                .AnyAsync(link => link.DepartmentId == departmentId && link.LocationId == locationId);
            var departmentExists = await dbContext.Departments.AnyAsync(item => item.Id == departmentId);
            var locationExists = await dbContext.Locations.AnyAsync(item => item.Id == locationId);

            linkExists.Should().BeFalse();
            departmentExists.Should().BeTrue();
            locationExists.Should().BeTrue();
        });
    }
}
