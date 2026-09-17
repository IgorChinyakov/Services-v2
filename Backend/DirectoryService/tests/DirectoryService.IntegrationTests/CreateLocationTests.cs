using DirectoryService.Application.Features.Locations.Create;
using DirectoryService.Contracts.Common;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.Shared;
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
    public async Task CreateLocation_WithValidAddress_ShouldPersistLocation()
    {
        // Arrange
        var command = new CreateLocationCommand(
            "Main Office",
            new AddressDto
            {
                City = "Moscow",
                Street = "Tverskaya",
                House = "12A",
                Apartment = "Office 45",
            });

        // Act
        var result = await ExecuteCommandAsync<CreateLocationCommand, Guid>(command);

        // Assert
        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.GetMessage() : string.Empty);
        await ExecuteDbContextAsync(async dbContext =>
        {
            var location = await dbContext.Locations
                .FirstAsync(item => item.Id == LocationId.Create(result.Value));

            location.Name.Value.Should().Be(command.Name);
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
        var command = new CreateLocationCommand(
            "Main Office",
            new AddressDto
            {
                City = string.Empty,
                Street = " ",
                House = "1",
                Apartment = string.Empty,
            });

        // Act
        var result = await ExecuteCommandAsync<CreateLocationCommand, Guid>(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation, result.Error.GetMessage());
        result.Error.Messages.Select(message => message.InvalidField).Should().BeEquivalentTo(
            "Address.City",
            "Address.Street",
            "Address.House",
            "Address.Apartment");
    }
}
