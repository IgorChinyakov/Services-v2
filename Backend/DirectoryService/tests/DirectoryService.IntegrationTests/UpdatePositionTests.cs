using DirectoryService.Application.Features.Positions.Update;
using DirectoryService.Domain.Shared;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public sealed class UpdatePositionTests : DirectoryServiceTestsBase
{
    public UpdatePositionTests(DirectoryServiceWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task UpdatePosition_WithValidName_ShouldRenamePosition()
    {
        // Arrange
        var departmentId = await SeedDepartmentAsync("Development Department", "development");
        var positionId = await SeedPositionAsync("Developer", [departmentId]);
        var command = new UpdatePositionCommand(positionId.Value, "Senior Developer");

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.GetMessage() : string.Empty);
        await ExecuteDbContextAsync(async dbContext =>
        {
            var position = await dbContext.Positions.SingleAsync(item => item.Id == positionId);
            position.Name.Value.Should().Be("Senior Developer");
        });
    }

    [Fact]
    public async Task UpdatePosition_WithDuplicateActiveName_ShouldReturnConflict()
    {
        // Arrange
        var departmentId = await SeedDepartmentAsync("Development Department", "development");
        await SeedPositionAsync("Developer", [departmentId]);
        var positionId = await SeedPositionAsync("Designer", [departmentId]);
        var command = new UpdatePositionCommand(positionId.Value, "Developer");

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Messages.Should().ContainSingle(message => message.Code == "position.name.duplicate");
    }

    [Fact]
    public async Task UpdatePosition_WhenPositionDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var command = new UpdatePositionCommand(Guid.NewGuid(), "Developer");

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Messages.Should().ContainSingle(message => message.Code == "position.not_found");
    }
}
