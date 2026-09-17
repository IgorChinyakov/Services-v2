using System.Net;
using System.Net.Http.Json;
using DirectoryService.Domain.Entities;
using DirectoryService.Presentation.ApiResponse;
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
        using var client = CreateClient();

        // Act
        var response = await client.PostAsync(
            $"/api/departments/{departmentId.Value}/locations/{locationId.Value}",
            null);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope!.IsError.Should().BeFalse();
        await ExecuteDbContextAsync(async dbContext =>
        {
            (await dbContext.Set<DepartmentLocation>().AnyAsync(link =>
                link.DepartmentId == departmentId && link.LocationId == locationId)).Should().BeTrue();
        });
    }

    [Fact]
    public async Task AddDepartmentLocation_WhenLinkAlreadyExists_ShouldReturnConflictWithoutDuplicate()
    {
        // Arrange
        var locationId = await SeedLocationAsync();
        var departmentId = await SeedDepartmentAsync("Sales Department", "sales", locationIds: [locationId]);
        using var client = CreateClient();

        // Act
        var response = await client.PostAsync(
            $"/api/departments/{departmentId.Value}/locations/{locationId.Value}",
            null);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        envelope!.Errors.Should().ContainSingle(error => error.Code == "department.location.duplicate");
        await ExecuteDbContextAsync(async dbContext =>
        {
            (await dbContext.Set<DepartmentLocation>().CountAsync(link =>
                link.DepartmentId == departmentId && link.LocationId == locationId)).Should().Be(1);
        });
    }

    [Fact]
    public async Task AddDepartmentLocation_WhenLocationDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var departmentId = await SeedDepartmentAsync("Sales Department", "sales", locationIds: []);
        using var client = CreateClient();

        // Act
        var response = await client.PostAsync(
            $"/api/departments/{departmentId.Value}/locations/{Guid.NewGuid()}",
            null);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        envelope!.Errors.Should().ContainSingle(error => error.Code == "location.not_found");
    }

    [Fact]
    public async Task AddDepartmentLocation_WhenDepartmentDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var locationId = await SeedLocationAsync();
        using var client = CreateClient();

        // Act
        var response = await client.PostAsync(
            $"/api/departments/{Guid.NewGuid()}/locations/{locationId.Value}",
            null);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        envelope!.Errors.Should().ContainSingle(error => error.Code == "department.not_found");
    }

    [Fact]
    public async Task RemoveDepartmentLocation_WhenLinkExists_ShouldDeleteOnlyLink()
    {
        // Arrange
        var locationId = await SeedLocationAsync();
        var departmentId = await SeedDepartmentAsync("Sales Department", "sales", locationIds: [locationId]);
        using var client = CreateClient();
        var endpoint = $"/api/departments/{departmentId.Value}/locations/{locationId.Value}";

        // Act
        var firstResponse = await client.DeleteAsync(endpoint);
        var secondResponse = await client.DeleteAsync(endpoint);
        var secondEnvelope = await secondResponse.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        secondEnvelope!.Errors.Should().ContainSingle(error => error.Code == "department.location.not_found");
        await ExecuteDbContextAsync(async dbContext =>
        {
            (await dbContext.Set<DepartmentLocation>().AnyAsync(link =>
                link.DepartmentId == departmentId && link.LocationId == locationId)).Should().BeFalse();
            (await dbContext.Departments.AnyAsync(item => item.Id == departmentId)).Should().BeTrue();
            (await dbContext.Locations.AnyAsync(item => item.Id == locationId)).Should().BeTrue();
        });
    }
}
