using DirectoryService.Application.Features.Departments.AddPosition;
using DirectoryService.Application.Features.Departments.RemovePosition;
using DirectoryService.Domain.Entities;
using DirectoryService.Domain.Shared;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public sealed class DepartmentPositionLinkTests : DirectoryServiceTestsBase
{
    public DepartmentPositionLinkTests(DirectoryServiceWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task AddDepartmentPosition_WhenEntitiesExist_ShouldCreateLink()
    {
        // Arrange
        var departmentId = await SeedDepartmentAsync("Sales Department", "sales");
        var positionId = await SeedPositionAsync("Sales Manager", []);
        var command = new AddDepartmentPositionCommand(departmentId.Value, positionId.Value);

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.GetMessage() : string.Empty);
        await ExecuteDbContextAsync(async dbContext =>
        {
            var linkExists = await dbContext.Set<DepartmentPosition>()
                .AnyAsync(link => link.DepartmentId == departmentId && link.PositionId == positionId);

            linkExists.Should().BeTrue();
        });
    }

    [Fact]
    public async Task AddDepartmentPosition_WhenLinkExists_ShouldReturnConflict()
    {
        // Arrange
        var departmentId = await SeedDepartmentAsync("Sales Department", "sales");
        var positionId = await SeedPositionAsync("Sales Manager", [departmentId]);
        var command = new AddDepartmentPositionCommand(departmentId.Value, positionId.Value);

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Messages.Should().ContainSingle(message =>
            message.Code == "department.position.duplicate");
    }

    [Fact]
    public async Task AddDepartmentPosition_WhenPositionDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var departmentId = await SeedDepartmentAsync("Sales Department", "sales");
        var command = new AddDepartmentPositionCommand(departmentId.Value, Guid.NewGuid());

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Messages.Should().ContainSingle(message => message.Code == "position.not_found");
    }

    [Fact]
    public async Task RemoveDepartmentPosition_WhenLinkExists_ShouldDeleteLinkOnly()
    {
        // Arrange
        var departmentId = await SeedDepartmentAsync("Sales Department", "sales");
        var positionId = await SeedPositionAsync("Sales Manager", [departmentId]);
        var command = new RemoveDepartmentPositionCommand(departmentId.Value, positionId.Value);

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.GetMessage() : string.Empty);
        await ExecuteDbContextAsync(async dbContext =>
        {
            (await dbContext.Set<DepartmentPosition>()
                    .AnyAsync(link => link.DepartmentId == departmentId && link.PositionId == positionId))
                .Should().BeFalse();
            (await dbContext.Departments.AnyAsync(item => item.Id == departmentId)).Should().BeTrue();
            (await dbContext.Positions.AnyAsync(item => item.Id == positionId)).Should().BeTrue();
        });
    }

    [Fact]
    public async Task RemoveDepartmentPosition_WhenLinkDoesNotExist_ShouldReturnClearNotFoundError()
    {
        // Arrange
        var departmentId = await SeedDepartmentAsync("Sales Department", "sales");
        var positionId = await SeedPositionAsync("Sales Manager", []);
        var command = new RemoveDepartmentPositionCommand(departmentId.Value, positionId.Value);

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Messages.Should().ContainSingle(message =>
            message.Code == "department.position.not_found");
    }
}
