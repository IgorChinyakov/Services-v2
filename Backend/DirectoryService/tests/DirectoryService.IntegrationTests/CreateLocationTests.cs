using System.Net;
using System.Net.Http.Json;
using DirectoryService.Contracts.Common;
using DirectoryService.Contracts.Locations.Requests;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Presentation.ApiResponse;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public sealed class CreateLocationTests : DirectoryServiceTestsBase
{
    public CreateLocationTests(DirectoryServiceWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CreateLocation_WithValidAddress_ShouldReturnCreatedAndPersistLocation()
    {
        // Arrange
        using var client = CreateClient();
        var request = new CreateLocationRequest
        {
            Name = "Main Office",
            Address = new AddressDto
            {
                City = "Moscow",
                Street = "Tverskaya",
                House = "12A",
                Apartment = "Office 45",
            },
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/locations", request);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        envelope.Should().NotBeNull();
        envelope!.IsError.Should().BeFalse();

        var locationId = Guid.Parse(envelope.Result!.ToString()!);
        await ExecuteDbContextAsync(async dbContext =>
        {
            var location = await dbContext.Locations
                .FirstAsync(item => item.Id == LocationId.Create(locationId));

            location.Name.Value.Should().Be(request.Name);
            location.Address.City.Should().Be("Moscow");
            location.Address.Street.Should().Be("Tverskaya");
            location.Address.House.Should().Be("12A");
            location.Address.Apartment.Should().Be("Office 45");
        });
    }

    [Fact]
    public async Task CreateLocation_WithInvalidAddress_ShouldReturnAllAddressErrors()
    {
        // Arrange
        using var client = CreateClient();
        var request = new CreateLocationRequest
        {
            Name = "Main Office",
            Address = new AddressDto
            {
                City = string.Empty,
                Street = " ",
                House = string.Empty,
                Apartment = string.Empty,
            },
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/locations", request);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        envelope.Should().NotBeNull();
        envelope!.Errors.Select(error => error.InvalidField).Should().BeEquivalentTo(
            "Address.City",
            "Address.Street",
            "Address.House",
            "Address.Apartment");
        await ExecuteDbContextAsync(async dbContext =>
        {
            (await dbContext.Locations.CountAsync()).Should().Be(0);
        });
    }
}
