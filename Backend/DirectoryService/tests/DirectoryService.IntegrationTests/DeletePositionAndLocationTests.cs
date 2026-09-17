using System.Net;
using System.Net.Http.Json;
using DirectoryService.Domain.Entities;
using DirectoryService.Presentation.ApiResponse;
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
    public async Task DeletePosition_WhenLinked_ShouldSoftDeletePositionAndPreserveLinks()
    {
        // Arrange
        var departmentId = await SeedDepartmentAsync("Development Department", "development");
        var positionId = await SeedPositionAsync("Developer", [departmentId]);
        using var client = CreateClient();

        // Act
        var response = await client.DeleteAsync($"/api/positions/{positionId.Value}");
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope!.IsError.Should().BeFalse();
        await ExecuteDbContextAsync(async dbContext =>
        {
            var position = await dbContext.Positions
                .IgnoreQueryFilters()
                .SingleAsync(item => item.Id == positionId);

            position.IsActive.Should().BeFalse();
            position.DeletedAt.Should().NotBeNull();
            (await dbContext.Set<DepartmentPosition>()
                    .IgnoreQueryFilters()
                    .AnyAsync(link => link.PositionId == positionId))
                .Should().BeTrue();
            (await dbContext.Departments.AnyAsync(item => item.Id == departmentId)).Should().BeTrue();
        });
    }

    [Fact]
    public async Task DeleteLocation_WhenLinked_ShouldSoftDeleteLocationAndPreserveLinks()
    {
        // Arrange
        var locationId = await SeedLocationAsync();
        var departmentId = await SeedDepartmentAsync("Sales Department", "sales", locationIds: [locationId]);
        using var client = CreateClient();

        // Act
        var response = await client.DeleteAsync($"/api/locations/{locationId.Value}");
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope!.IsError.Should().BeFalse();
        await ExecuteDbContextAsync(async dbContext =>
        {
            var location = await dbContext.Locations
                .IgnoreQueryFilters()
                .SingleAsync(item => item.Id == locationId);

            location.IsActive.Should().BeFalse();
            location.DeletedAt.Should().NotBeNull();
            (await dbContext.Set<DepartmentLocation>()
                    .IgnoreQueryFilters()
                    .AnyAsync(link => link.LocationId == locationId))
                .Should().BeTrue();
            (await dbContext.Departments.AnyAsync(item => item.Id == departmentId)).Should().BeTrue();
        });
    }

    [Theory]
    [InlineData("positions", "position.not_found")]
    [InlineData("locations", "location.not_found")]
    public async Task DeleteEntity_WhenEntityDoesNotExist_ShouldReturnNotFound(
        string resource,
        string errorCode)
    {
        // Arrange
        using var client = CreateClient();

        // Act
        var response = await client.DeleteAsync($"/api/{resource}/{Guid.NewGuid()}");
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        envelope!.Errors.Should().ContainSingle(error => error.Code == errorCode);
    }
}
