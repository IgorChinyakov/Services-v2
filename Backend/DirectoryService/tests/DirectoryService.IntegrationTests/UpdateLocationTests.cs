using DirectoryService.Application.Features.Locations.Update;
using DirectoryService.Contracts.Common;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.Shared;
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
    public async Task UpdateLocation_WithValidData_ShouldChangeNameAndAddress()
    {
        // Arrange
        var locationId = await SeedLocationAsync("Old Office");
        var command = new UpdateLocationCommand(
            locationId.Value,
            "New Office",
            new AddressDto
            {
                City = "Yerevan",
                Street = "Northern Avenue",
                House = "10",
                Apartment = "Office 5",
            });

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.GetMessage() : string.Empty);
        await ExecuteDbContextAsync(async dbContext =>
        {
            var location = await dbContext.Locations.FirstAsync(item => item.Id == locationId);

            location.Name.Value.Should().Be(command.Name);
            location.Address.City.Should().Be(command.Address.City);
            location.Address.Street.Should().Be(command.Address.Street);
            location.Address.House.Should().Be(command.Address.House);
            location.Address.Apartment.Should().Be(command.Address.Apartment);
        });
    }

    [Fact]
    public async Task UpdateLocation_WithDuplicateName_ShouldReturnConflictAndKeepOldData()
    {
        // Arrange
        await SeedLocationAsync("Main Office");
        var locationId = await SeedLocationAsync("Branch Office");
        var command = new UpdateLocationCommand(
            locationId.Value,
            "Main Office",
            new AddressDto
            {
                City = "Yerevan",
                Street = "Aram Street",
                House = "10",
                Apartment = "20",
            });

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Messages.Should().ContainSingle(message => message.Code == "location.name.duplicate");
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
            var location = await dbContext.Locations
                .FirstAsync(item => item.Id == occupiedLocationId);

            return new AddressDto
            {
                City = location.Address.City,
                Street = location.Address.Street,
                House = location.Address.House,
                Apartment = location.Address.Apartment,
            };
        });
        var command = new UpdateLocationCommand(
            locationId.Value,
            "Updated Branch Office",
            occupiedAddress);

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Messages.Should().ContainSingle(message => message.Code == "location.address.duplicate");
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
        var command = new UpdateLocationCommand(
            Guid.NewGuid(),
            "New Office",
            new AddressDto
            {
                City = "Yerevan",
                Street = "Aram Street",
                House = "10",
                Apartment = "20",
            });

        // Act
        var result = await ExecuteCommandAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Messages.Should().ContainSingle(message => message.Code == "location.not_found");
    }
}
