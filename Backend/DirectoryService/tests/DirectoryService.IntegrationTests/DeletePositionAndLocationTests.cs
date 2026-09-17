using DirectoryService.Application.Features.Locations.Delete;
using DirectoryService.Application.Features.Positions.Delete;
using DirectoryService.Domain.Entities;
using DirectoryService.Domain.Shared;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public sealed class DeletePositionAndLocationTests : DirectoryServiceTestsBase
{
    public DeletePositionAndLocationTests(DirectoryServiceWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task DeletePosition_WhenLinked_ShouldDeletePositionAndLinksOnly()
    {
        // Arrange
        var departmentId = await SeedDepartmentAsync("Development Department", "development");
        var positionId = await SeedPositionAsync("Developer", [departmentId]);
        var command = new DeletePositionCommand(positionId.Value);

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.GetMessage() : string.Empty);
        await ExecuteDbContextAsync(async dbContext =>
        {
            (await dbContext.Positions.AnyAsync(item => item.Id == positionId)).Should().BeFalse();
            (await dbContext.Set<DepartmentPosition>()
                    .AnyAsync(link => link.PositionId == positionId))
                .Should().BeFalse();
            (await dbContext.Departments.AnyAsync(item => item.Id == departmentId)).Should().BeTrue();
        });
    }

    [Fact]
    public async Task DeleteLocation_WhenLinked_ShouldDeleteLocationAndLinksOnly()
    {
        // Arrange
        var locationId = await SeedLocationAsync();
        var departmentId = await SeedDepartmentAsync(
            "Sales Department",
            "sales",
            locationIds: [locationId]);
        var command = new DeleteLocationCommand(locationId.Value);

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.GetMessage() : string.Empty);
        await ExecuteDbContextAsync(async dbContext =>
        {
            (await dbContext.Locations.AnyAsync(item => item.Id == locationId)).Should().BeFalse();
            (await dbContext.Set<DepartmentLocation>()
                    .AnyAsync(link => link.LocationId == locationId))
                .Should().BeFalse();
            (await dbContext.Departments.AnyAsync(item => item.Id == departmentId)).Should().BeTrue();
        });
    }

    [Fact]
    public async Task DeletePosition_WhenPositionDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var command = new DeletePositionCommand(Guid.NewGuid());

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Messages.Should().ContainSingle(message => message.Code == "position.not_found");
    }

    [Fact]
    public async Task DeleteLocation_WhenLocationDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var command = new DeleteLocationCommand(Guid.NewGuid());

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Messages.Should().ContainSingle(message => message.Code == "location.not_found");
    }
}
