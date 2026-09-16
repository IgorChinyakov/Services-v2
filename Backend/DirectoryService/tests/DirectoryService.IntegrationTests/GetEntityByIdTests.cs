using System.Net;
using System.Net.Http.Json;
using DirectoryService.Application.Abstractions.Database;
using DirectoryService.Contracts.Departments;
using DirectoryService.Contracts.Locations;
using DirectoryService.Domain.Shared;
using DirectoryService.Infrastructure.Database;
using DirectoryService.Presentation.ApiResponse;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public sealed class GetEntityByIdTests : DirectoryServiceTestsBase
{
    private readonly DirectoryServiceWebFactory _factory;

    public GetEntityByIdTests(DirectoryServiceWebFactory factory)
        : base(factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetLocationById_WhenLocationExists_ShouldReturnLocationDto()
    {
        // Arrange
        var locationId = await SeedLocationAsync("Main Office");
        using var client = _factory.CreateClient();

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
        envelope.Result.Timezone.Should().Be("Europe/Moscow");
        envelope.Result.Address.Country.Should().Be("Country 1");
    }

    [Fact]
    public async Task GetLocationById_WhenLocationDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/locations/{Guid.NewGuid()}");
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        envelope.Should().NotBeNull();
        envelope!.IsError.Should().BeTrue();
        envelope.Error!.Type.Should().Be(ErrorType.NotFound);
        envelope.Error.Messages.Should().ContainSingle(message =>
            message.Code == "location.not_found");
    }

    [Fact]
    public async Task GetDepartmentById_WhenDepartmentExists_ShouldReturnDepartmentDto()
    {
        // Arrange
        var departmentId = await SeedDepartmentAsync("Development Department", "development");
        using var client = _factory.CreateClient();

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
        envelope.Result.Identifier.Should().Be("development");
        envelope.Result.Path.Should().Be("development");
        envelope.Result.ParentId.Should().BeNull();
    }

    [Fact]
    public async Task GetDepartmentById_WhenDepartmentDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/departments/{Guid.NewGuid()}");
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        envelope.Should().NotBeNull();
        envelope!.IsError.Should().BeTrue();
        envelope.Error!.Type.Should().Be(ErrorType.NotFound);
        envelope.Error.Messages.Should().ContainSingle(message =>
            message.Code == "department.not_found");
    }

    [Fact]
    public async Task ReadDbContext_WhenEntityIsLoaded_ShouldNotTrackIt()
    {
        // Arrange
        var locationId = await SeedLocationAsync();

        // Act
        var trackedEntriesCount = await ExecuteScopedAsync(async services =>
        {
            var readDbContext = services.GetRequiredService<IReadDbContext>();
            var dbContext = services.GetRequiredService<DirectoryServiceDbContext>();

            _ = await readDbContext.Locations
                .SingleAsync(location => location.Id == locationId);

            return dbContext.ChangeTracker.Entries().Count();
        });

        // Assert
        trackedEntriesCount.Should().Be(0);
    }
}
