using System.Net;
using System.Net.Http.Json;
using DirectoryService.Contracts.Common;
using DirectoryService.Contracts.Locations.Requests;
using DirectoryService.Contracts.Positions.Requests;
using DirectoryService.Presentation.ApiResponse;
using FluentAssertions;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public sealed class BusinessErrorCodeTests : DirectoryServiceTestsBase
{
    private readonly DirectoryServiceWebFactory _factory;

    public BusinessErrorCodeTests(DirectoryServiceWebFactory factory)
        : base(factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateLocation_WithDuplicateName_ShouldReturnSpecificConflictCode()
    {
        // Arrange
        await SeedLocationAsync("Main Office");
        using var client = _factory.CreateClient();
        var request = new CreateLocationRequest
        {
            Name = "Main Office",
            Address = CreateAddress(2),
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/locations", request);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        envelope.Should().NotBeNull();
        envelope!.Errors.Should().ContainSingle(message =>
            message.Code == "location.name.duplicate");
    }

    [Fact]
    public async Task CreateLocation_WithDuplicateAddress_ShouldReturnSpecificConflictCode()
    {
        // Arrange
        await SeedLocationAsync("Main Office");
        using var client = _factory.CreateClient();
        var request = new CreateLocationRequest
        {
            Name = "Branch Office",
            Address = CreateAddress(1),
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/locations", request);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        envelope.Should().NotBeNull();
        envelope!.Errors.Should().ContainSingle(message =>
            message.Code == "location.address.duplicate");
    }

    [Fact]
    public async Task CreatePosition_WithDuplicateActiveName_ShouldReturnSpecificConflictCode()
    {
        // Arrange
        var departmentId = await SeedDepartmentAsync("Development Department", "development");
        await SeedPositionAsync("Developer", [departmentId]);
        using var client = _factory.CreateClient();
        var request = new CreatePositionRequest
        {
            Name = "Developer",
            DepartmentIds = [departmentId.Value],
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/positions", request);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        envelope.Should().NotBeNull();
        envelope!.Errors.Should().ContainSingle(message =>
            message.Code == "position.name.duplicate");
    }

    [Fact]
    public async Task CreatePosition_WithMissingDepartment_ShouldReturnSpecificNotFoundCode()
    {
        // Arrange
        using var client = _factory.CreateClient();
        var request = new CreatePositionRequest
        {
            Name = "Developer",
            DepartmentIds = [Guid.NewGuid()],
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/positions", request);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        envelope.Should().NotBeNull();
        envelope!.Errors.Should().ContainSingle(message =>
            message.Code == "department.not_found");
    }

    private static AddressDto CreateAddress(int number) =>
        new()
        {
            City = $"City {number}",
            Street = $"Street {number}",
            House = $"House {number}",
            Apartment = $"Apartment {number}",
        };
}
