using System.Net;
using System.Net.Http.Json;
using DirectoryService.Contracts.Positions.Requests;
using DirectoryService.Presentation.ApiResponse;
using FluentAssertions;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public sealed class Ds14CrudEndpointTests : DirectoryServiceTestsBase
{
    private readonly DirectoryServiceWebFactory _factory;

    public Ds14CrudEndpointTests(DirectoryServiceWebFactory factory)
        : base(factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PatchPosition_ShouldReturnSuccessfulEnvelope()
    {
        // Arrange
        var departmentId = await SeedDepartmentAsync("Development Department", "development");
        var positionId = await SeedPositionAsync("Developer", [departmentId]);
        using var client = _factory.CreateClient();
        var request = new UpdatePositionRequest { Name = "Senior Developer" };

        // Act
        var response = await client.PatchAsJsonAsync($"/api/positions/{positionId.Value}", request);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope.Should().NotBeNull();
        envelope!.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task DeletePosition_ShouldReturnSuccessfulEnvelope()
    {
        // Arrange
        var departmentId = await SeedDepartmentAsync("Development Department", "development");
        var positionId = await SeedPositionAsync("Developer", [departmentId]);
        using var client = _factory.CreateClient();

        // Act
        var response = await client.DeleteAsync($"/api/positions/{positionId.Value}");
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope.Should().NotBeNull();
        envelope!.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteLocation_ShouldReturnSuccessfulEnvelope()
    {
        // Arrange
        var locationId = await SeedLocationAsync();
        using var client = _factory.CreateClient();

        // Act
        var response = await client.DeleteAsync($"/api/locations/{locationId.Value}");
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope.Should().NotBeNull();
        envelope!.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task AddDepartmentPosition_ShouldReturnSuccessfulEnvelope()
    {
        // Arrange
        var departmentId = await SeedDepartmentAsync("Sales Department", "sales");
        var positionId = await SeedPositionAsync("Sales Manager", []);
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsync(
            $"/api/departments/{departmentId.Value}/positions/{positionId.Value}",
            null);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope.Should().NotBeNull();
        envelope!.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task AddDepartmentPosition_WhenLinkExists_ShouldReturnConflictEnvelope()
    {
        // Arrange
        var departmentId = await SeedDepartmentAsync("Sales Department", "sales");
        var positionId = await SeedPositionAsync("Sales Manager", [departmentId]);
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsync(
            $"/api/departments/{departmentId.Value}/positions/{positionId.Value}",
            null);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        envelope.Should().NotBeNull();
        envelope!.Errors.Should().ContainSingle(error =>
            error.Code == "department.position.duplicate");
    }

    [Fact]
    public async Task RemoveDepartmentPosition_ShouldReturnSuccessfulEnvelope()
    {
        // Arrange
        var departmentId = await SeedDepartmentAsync("Sales Department", "sales");
        var positionId = await SeedPositionAsync("Sales Manager", [departmentId]);
        using var client = _factory.CreateClient();

        // Act
        var response = await client.DeleteAsync(
            $"/api/departments/{departmentId.Value}/positions/{positionId.Value}");
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope.Should().NotBeNull();
        envelope!.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveDepartmentPosition_WhenLinkDoesNotExist_ShouldReturnNotFoundEnvelope()
    {
        // Arrange
        var departmentId = await SeedDepartmentAsync("Sales Department", "sales");
        var positionId = await SeedPositionAsync("Sales Manager", []);
        using var client = _factory.CreateClient();

        // Act
        var response = await client.DeleteAsync(
            $"/api/departments/{departmentId.Value}/positions/{positionId.Value}");
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        envelope.Should().NotBeNull();
        envelope!.Errors.Should().ContainSingle(error =>
            error.Code == "department.position.not_found");
    }
}
