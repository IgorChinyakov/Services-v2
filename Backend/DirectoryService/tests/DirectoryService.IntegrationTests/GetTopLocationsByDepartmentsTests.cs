using System.Net;
using System.Net.Http.Json;
using DirectoryService.Contracts.Locations;
using DirectoryService.Presentation.ApiResponse;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public sealed class GetTopLocationsByDepartmentsTests : DirectoryServiceTestsBase
{
    public GetTopLocationsByDepartmentsTests(DirectoryServiceWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetTop_WithMoreThanFiveLocations_ShouldReturnFiveOrderedLocations()
    {
        // Arrange
        var locations = new[]
        {
            await SeedLocationAsync("Alpha Office"),
            await SeedLocationAsync("Bravo Office"),
            await SeedLocationAsync("Charlie Office"),
            await SeedLocationAsync("Delta Office"),
            await SeedLocationAsync("Echo Office"),
            await SeedLocationAsync("Foxtrot Office"),
        };

        for (var number = 0; number < locations.Length; number++)
        {
            await SeedDepartmentAsync(
                $"Department {number + 1}",
                $"department-{number + 1}",
                locationIds: locations.Take(locations.Length - number).ToArray());
        }

        using var client = CreateClient();

        // Act
        var response = await client.GetAsync("/api/locations/top");
        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<IReadOnlyList<TopLocationByDepartmentsDto>>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope.Should().NotBeNull();
        envelope!.IsError.Should().BeFalse();
        envelope.Result.Should().HaveCount(5);
        envelope.Result!.Select(location => location.Name)
            .Should().Equal("Alpha Office", "Bravo Office", "Charlie Office", "Delta Office", "Echo Office");
        envelope.Result.Select(location => location.DepartmentCount)
            .Should().Equal(6, 5, 4, 3, 2);
        envelope.Result[0].Address.Should().BeEquivalentTo(new
        {
            City = "City 1",
            Street = "Street 1",
            House = "House 1",
            Apartment = "Apartment 1",
        });
    }

    [Fact]
    public async Task GetTop_WithEmptyAndInactiveData_ShouldUseActiveEntitiesAndReturnZeroCounts()
    {
        // Arrange
        var populatedLocationId = await SeedLocationAsync("Populated Office");
        var emptyLocationId = await SeedLocationAsync("Empty Office");
        var inactiveDepartmentLocationId = await SeedLocationAsync("Inactive Department Office");
        var inactiveLocationId = await SeedLocationAsync("Inactive Office");

        await SeedDepartmentAsync(
            "Active Department",
            "active-department",
            locationIds: [populatedLocationId]);

        var inactiveDepartmentId = await SeedDepartmentAsync(
            "Inactive Department",
            "inactive-department",
            locationIds: [inactiveDepartmentLocationId]);

        await ExecuteDbContextAsync(async dbContext =>
        {
            await dbContext.Departments
                .Where(department => department.Id == inactiveDepartmentId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(department => department.IsActive, false));

            await dbContext.Locations
                .Where(location => location.Id == inactiveLocationId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(location => location.IsActive, false));
        });

        using var client = CreateClient();

        // Act
        var response = await client.GetAsync("/api/locations/top");
        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<IReadOnlyList<TopLocationByDepartmentsDto>>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope.Should().NotBeNull();
        envelope!.Result.Should().HaveCount(3);
        envelope.Result.Should().ContainSingle(location =>
            location.Id == populatedLocationId.Value && location.DepartmentCount == 1);
        envelope.Result.Should().ContainSingle(location =>
            location.Id == emptyLocationId.Value && location.DepartmentCount == 0);
        envelope.Result.Should().ContainSingle(location =>
            location.Id == inactiveDepartmentLocationId.Value && location.DepartmentCount == 0);
        envelope.Result.Should().NotContain(location => location.Id == inactiveLocationId.Value);
    }
}
