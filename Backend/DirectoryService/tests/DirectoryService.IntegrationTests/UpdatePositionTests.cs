using System.Net;
using System.Net.Http.Json;
using DirectoryService.Contracts.Positions.Requests;
using DirectoryService.Presentation.ApiResponse;
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
    public async Task UpdatePosition_WithValidName_ShouldReturnOkAndRenamePosition()
    {
        // Arrange
        var departmentId = await SeedDepartmentAsync("Development Department", "development");
        var positionId = await SeedPositionAsync("Developer", [departmentId]);
        using var client = CreateClient();

        // Act
        var response = await client.PatchAsJsonAsync(
            $"/api/positions/{positionId.Value}",
            new UpdatePositionRequest { Name = "Senior Developer" });
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope!.IsError.Should().BeFalse();
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
        using var client = CreateClient();

        // Act
        var response = await client.PatchAsJsonAsync(
            $"/api/positions/{positionId.Value}",
            new UpdatePositionRequest { Name = "Developer" });
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        envelope!.Errors.Should().ContainSingle(error => error.Code == "position.name.duplicate");
    }

    [Fact]
    public async Task UpdatePosition_WhenPositionDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        using var client = CreateClient();

        // Act
        var response = await client.PatchAsJsonAsync(
            $"/api/positions/{Guid.NewGuid()}",
            new UpdatePositionRequest { Name = "Developer" });
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        envelope!.Errors.Should().ContainSingle(error => error.Code == "position.not_found");
    }
}
