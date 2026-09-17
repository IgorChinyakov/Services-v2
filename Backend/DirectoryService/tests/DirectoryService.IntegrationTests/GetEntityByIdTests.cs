using System.Net;
using System.Net.Http.Json;
using DirectoryService.Contracts.Departments;
using DirectoryService.Contracts.Locations;
using DirectoryService.Presentation.ApiResponse;
using FluentAssertions;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public sealed class GetEntityByIdTests : DirectoryServiceTestsBase
{
    public GetEntityByIdTests(DirectoryServiceWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetLocationById_WhenLocationExists_ShouldReturnLocationDto()
    {
        // Arrange
        var locationId = await SeedLocationAsync("Main Office");
        using var client = CreateClient();

        // Act
        var response = await client.GetAsync($"/api/locations/{locationId.Value}");
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<LocationDetailsDto>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope.Should().NotBeNull();
        envelope!.IsError.Should().BeFalse();
        envelope.Result.Should().NotBeNull();
        envelope.Result!.Id.Should().Be(locationId.Value);
        envelope.Result.Name.Should().Be("Main Office");
        envelope.Result.Address.City.Should().Be("City 1");
        envelope.Result.Address.House.Should().Be("House 1");
        envelope.Result.Address.Apartment.Should().Be("Apartment 1");
    }

    [Fact]
    public async Task GetLocationById_WhenLocationDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        using var client = CreateClient();

        // Act
        var response = await client.GetAsync($"/api/locations/{Guid.NewGuid()}");
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        envelope.Should().NotBeNull();
        envelope!.IsError.Should().BeTrue();
        envelope.Errors.Should().ContainSingle(message =>
            message.Code == "location.not_found");
    }

    [Fact]
    public async Task GetLocationById_WhenLocationIsSoftDeleted_ShouldReturnNotFound()
    {
        // Arrange
        var locationId = await SeedLocationAsync("Deleted Office");
        using var client = CreateClient();
        var deleteResponse = await client.DeleteAsync($"/api/locations/{locationId.Value}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var response = await client.GetAsync($"/api/locations/{locationId.Value}");
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        envelope.Should().NotBeNull();
        envelope!.Errors.Should().ContainSingle(message => message.Code == "location.not_found");
    }

    [Fact]
    public async Task GetDepartmentById_WhenDepartmentExists_ShouldReturnDepartmentDto()
    {
        // Arrange
        var departmentId = await SeedDepartmentAsync("Development Department", "development");
        using var client = CreateClient();

        // Act
        var response = await client.GetAsync($"/api/departments/{departmentId.Value}");
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<DepartmentDetailsDto>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope.Should().NotBeNull();
        envelope!.IsError.Should().BeFalse();
        envelope.Result.Should().NotBeNull();
        envelope.Result!.Id.Should().Be(departmentId.Value);
        envelope.Result.Name.Should().Be("Development Department");
        envelope.Result.Slug.Should().Be("development");
        envelope.Result.Path.Should().Be("development");
        envelope.Result.ParentId.Should().BeNull();
    }

    [Fact]
    public async Task GetDepartmentById_WhenDepartmentDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        using var client = CreateClient();

        // Act
        var response = await client.GetAsync($"/api/departments/{Guid.NewGuid()}");
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        envelope.Should().NotBeNull();
        envelope!.IsError.Should().BeTrue();
        envelope.Errors.Should().ContainSingle(message =>
            message.Code == "department.not_found");
    }

    [Fact]
    public async Task GetDepartmentById_WhenDepartmentIsSoftDeleted_ShouldReturnNotFound()
    {
        // Arrange
        var departmentId = await SeedDepartmentAsync("Deleted Department", "deleted-department");
        using var client = CreateClient();
        var deleteResponse = await client.DeleteAsync($"/api/departments/{departmentId.Value}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var response = await client.GetAsync($"/api/departments/{departmentId.Value}");
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        envelope.Should().NotBeNull();
        envelope!.Errors.Should().ContainSingle(message => message.Code == "department.not_found");
    }

}
