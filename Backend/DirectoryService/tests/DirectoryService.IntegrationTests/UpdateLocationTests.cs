using System.Net;
using System.Net.Http.Json;
using DirectoryService.Contracts.Common;
using DirectoryService.Contracts.Locations.Requests;
using DirectoryService.Presentation.ApiResponse;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public sealed class UpdateLocationTests : DirectoryServiceTestsBase
{
    public UpdateLocationTests(DirectoryServiceWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task UpdateLocation_WithValidData_ShouldReturnOkAndChangeLocation()
    {
        // Arrange
        var locationId = await SeedLocationAsync("Old Office");
        using var client = CreateClient();
        var request = CreateRequest("New Office");

        // Act
        var response = await client.PatchAsJsonAsync($"/api/locations/{locationId.Value}", request);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope!.IsError.Should().BeFalse();
        await ExecuteDbContextAsync(async dbContext =>
        {
            var location = await dbContext.Locations.FirstAsync(item => item.Id == locationId);
            location.Name.Value.Should().Be(request.Name);
            location.Address.City.Should().Be(request.Address.City);
            location.Address.Street.Should().Be(request.Address.Street);
            location.Address.House.Should().Be(request.Address.House);
            location.Address.Apartment.Should().Be(request.Address.Apartment);
        });
    }

    [Fact]
    public async Task UpdateLocation_WithDuplicateName_ShouldReturnConflictAndKeepOldData()
    {
        // Arrange
        await SeedLocationAsync("Main Office");
        var locationId = await SeedLocationAsync("Branch Office");
        using var client = CreateClient();

        // Act
        var response = await client.PatchAsJsonAsync(
            $"/api/locations/{locationId.Value}",
            CreateRequest("Main Office"));
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        envelope!.Errors.Should().ContainSingle(error => error.Code == "location.name.duplicate");
        await ExecuteDbContextAsync(async dbContext =>
        {
            var location = await dbContext.Locations.FirstAsync(item => item.Id == locationId);
            location.Name.Value.Should().Be("Branch Office");
        });
    }

    [Fact]
    public async Task UpdateLocation_WithDuplicateAddress_ShouldReturnConflictAndKeepOldData()
    {
        // Arrange
        var occupiedLocationId = await SeedLocationAsync("Main Office");
        var locationId = await SeedLocationAsync("Branch Office");
        var occupiedAddress = await ExecuteDbContextAsync(async dbContext =>
        {
            var location = await dbContext.Locations.FirstAsync(item => item.Id == occupiedLocationId);
            return new AddressDto
            {
                City = location.Address.City,
                Street = location.Address.Street,
                House = location.Address.House,
                Apartment = location.Address.Apartment,
            };
        });
        using var client = CreateClient();
        var request = new UpdateLocationRequest
        {
            Name = "Updated Branch Office",
            Address = occupiedAddress,
        };

        // Act
        var response = await client.PatchAsJsonAsync($"/api/locations/{locationId.Value}", request);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        envelope!.Errors.Should().ContainSingle(error => error.Code == "location.address.duplicate");
        await ExecuteDbContextAsync(async dbContext =>
        {
            var location = await dbContext.Locations.FirstAsync(item => item.Id == locationId);
            location.Name.Value.Should().Be("Branch Office");
        });
    }

    [Fact]
    public async Task UpdateLocation_WhenLocationDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        using var client = CreateClient();

        // Act
        var response = await client.PatchAsJsonAsync(
            $"/api/locations/{Guid.NewGuid()}",
            CreateRequest("New Office"));
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        envelope!.Errors.Should().ContainSingle(error => error.Code == "location.not_found");
    }

    private static UpdateLocationRequest CreateRequest(string name) =>
        new()
        {
            Name = name,
            Address = new AddressDto
            {
                City = "Yerevan",
                Street = "Northern Avenue",
                House = "10",
                Apartment = "Office 5",
            },
        };
}
