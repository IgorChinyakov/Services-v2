using System.Net;
using System.Net.Http.Json;
using DirectoryService.Contracts.Departments.Requests;
using DirectoryService.Domain.Entities;
using DirectoryService.Presentation.ApiResponse;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public sealed class UpdateDepartmentLocationsTests : DirectoryServiceTestsBase
{
    public UpdateDepartmentLocationsTests(DirectoryServiceWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task UpdateDepartmentLocations_WithValidData_ShouldReplaceLocations()
    {
        // Arrange
        var oldLocationId = await SeedLocationAsync();
        var newLocationId = await SeedLocationAsync();
        var departmentId = await SeedDepartmentAsync("Sales Department", "sales", locationIds: [oldLocationId]);
        using var client = CreateClient();
        var request = new UpdateDepartmentLocationsRequest { LocationIds = [newLocationId.Value] };

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/departments/{departmentId.Value}/locations",
            request);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope!.IsError.Should().BeFalse();
        await ExecuteDbContextAsync(async dbContext =>
        {
            var department = await dbContext.Departments
                .Include(item => item.DepartmentLocations)
                .FirstAsync(item => item.Id == departmentId);

            department.DepartmentLocations.Select(link => link.LocationId)
                .Should().BeEquivalentTo([newLocationId]);
        });
    }

    [Fact]
    public async Task UpdateDepartmentLocations_WithMissingDepartment_ShouldReturnNotFound()
    {
        // Arrange
        var locationId = await SeedLocationAsync();
        using var client = CreateClient();

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/departments/{Guid.NewGuid()}/locations",
            new UpdateDepartmentLocationsRequest { LocationIds = [locationId.Value] });
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        envelope!.Errors.Should().ContainSingle(error => error.Code == "department.not_found");
    }

    [Fact]
    public async Task UpdateDepartmentLocations_WithMissingLocation_ShouldKeepOldLocations()
    {
        // Arrange
        var oldLocationId = await SeedLocationAsync();
        var departmentId = await SeedDepartmentAsync("Sales Department", "sales", locationIds: [oldLocationId]);
        using var client = CreateClient();

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/departments/{departmentId.Value}/locations",
            new UpdateDepartmentLocationsRequest { LocationIds = [Guid.NewGuid()] });
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        envelope!.Errors.Should().ContainSingle(error => error.Code == "location.not_found");
        await ExecuteDbContextAsync(async dbContext =>
        {
            var department = await dbContext.Departments
                .Include(item => item.DepartmentLocations)
                .FirstAsync(item => item.Id == departmentId);

            department.DepartmentLocations.Select(link => link.LocationId)
                .Should().BeEquivalentTo([oldLocationId]);
        });
    }

    [Fact]
    public async Task UpdateDepartmentLocations_WithDuplicateIds_ShouldReturnBadRequest()
    {
        // Arrange
        var locationId = await SeedLocationAsync();
        var departmentId = await SeedDepartmentAsync("Sales Department", "sales", locationIds: [locationId]);
        using var client = CreateClient();

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/departments/{departmentId.Value}/locations",
            new UpdateDepartmentLocationsRequest { LocationIds = [locationId.Value, locationId.Value] });
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        envelope!.Errors.Should().ContainSingle(error => error.InvalidField == "LocationIds");
    }

    [Fact]
    public async Task UpdateDepartmentLocations_WhenOldLinkIsHiddenByQueryFilter_ShouldReplaceAllLinks()
    {
        // Arrange
        var deletedLocationId = await SeedLocationAsync("Deleted office");
        var activeLocationId = await SeedLocationAsync("Active office");
        var departmentId = await SeedDepartmentAsync(
            "Sales Department",
            "sales",
            locationIds: [deletedLocationId]);

        using var client = CreateClient();
        var deleteResponse = await client.DeleteAsync($"/api/locations/{deletedLocationId.Value}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/departments/{departmentId.Value}/locations",
            new UpdateDepartmentLocationsRequest { LocationIds = [activeLocationId.Value] });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await ExecuteDbContextAsync(async dbContext =>
        {
            var links = await dbContext.Set<DepartmentLocation>()
                .IgnoreQueryFilters()
                .Where(link => link.DepartmentId == departmentId)
                .ToListAsync();

            links.Should().ContainSingle(link => link.LocationId == activeLocationId);
            links.Should().NotContain(link => link.LocationId == deletedLocationId);
        });
    }
}
