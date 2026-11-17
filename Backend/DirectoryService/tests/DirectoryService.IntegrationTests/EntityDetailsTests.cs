using System.Net;
using System.Net.Http.Json;
using DirectoryService.Contracts.Locations;
using DirectoryService.Contracts.Positions;
using DirectoryService.Presentation.ApiResponse;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public sealed class EntityDetailsTests : DirectoryServiceTestsBase
{
    private readonly DirectoryServiceWebFactory _factory;

    public EntityDetailsTests(DirectoryServiceWebFactory factory)
        : base(factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetLocationById_ShouldReturnCardDetailsAndActiveDepartmentCount()
    {
        var locationId = await SeedLocationAsync("Found Office");
        await SeedDepartmentAsync("Active Department", "active", locationIds: [locationId]);
        var inactiveDepartment = await SeedDepartmentAsync("Inactive Department", "inactive", locationIds: [locationId]);
        await ExecuteDbContextAsync(async dbContext =>
        {
            await dbContext.Departments.Where(item => item.Id == inactiveDepartment)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.IsActive, false));
        });

        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/locations/{locationId.Value}");
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<LocationDetailsDto>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope!.IsError.Should().BeFalse();
        envelope.Result!.Id.Should().Be(locationId.Value);
        envelope.Result.Name.Should().Be("Found Office");
        envelope.Result.Address.City.Should().NotBeNullOrEmpty();
        envelope.Result.DepartmentCount.Should().Be(1);
        envelope.Result.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetPositionById_ShouldReturnRequestedPositionWithoutDescription()
    {
        var departmentId = await SeedDepartmentAsync("Department", "department");
        var positionId = await SeedPositionAsync("Found Position", [departmentId]);
        await SeedPositionAsync("Other Position", [departmentId]);

        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/positions/{positionId.Value}");
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<PositionListItemDto>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope!.IsError.Should().BeFalse();
        envelope.Result!.Id.Should().Be(positionId.Value);
        envelope.Result.Name.Should().Be("Found Position");
        envelope.Result.Description.Should().BeNull();
        envelope.Result.CreatedAt.Should().BeAfter(DateTime.UtcNow.AddMinutes(-5));
    }

    [Fact]
    public async Task GetPositionById_ShouldReturnDescription()
    {
        var departmentId = await SeedDepartmentAsync("Department", "department");
        var positionId = await SeedPositionAsync("Described Position", [departmentId]);
        await ExecuteDbContextAsync(async dbContext =>
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE positions SET description = {"Position description"} WHERE id = {positionId.Value}");
        });

        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/positions/{positionId.Value}");
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<PositionListItemDto>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope!.Result!.Description.Should().Be("Position description");
    }

    [Fact]
    public async Task GetPositionById_ShouldHideInactivePosition()
    {
        var departmentId = await SeedDepartmentAsync("Department", "department");
        var positionId = await SeedPositionAsync("Inactive Position", [departmentId], isActive: false);

        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/positions/{positionId.Value}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("locations")]
    [InlineData("positions")]
    public async Task GetById_ShouldHideArchivedRecord(string entity)
    {
        Guid id;
        if (entity == "locations")
        {
            var locationId = await SeedLocationAsync("Archived Office");
            id = locationId.Value;
            await ExecuteDbContextAsync(async dbContext =>
            {
                var location = await dbContext.Locations.SingleAsync(item => item.Id == locationId);
                location.SoftDelete();
                await dbContext.SaveChangesAsync();
            });
        }
        else
        {
            var departmentId = await SeedDepartmentAsync("Department", "department");
            var positionId = await SeedPositionAsync("Archived Position", [departmentId]);
            id = positionId.Value;
            await ExecuteDbContextAsync(async dbContext =>
            {
                var position = await dbContext.Positions.SingleAsync(item => item.Id == positionId);
                position.SoftDelete();
                await dbContext.SaveChangesAsync();
            });
        }

        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/{entity}/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("locations")]
    [InlineData("positions")]
    public async Task GetById_UnknownId_ShouldReturnNotFound(string entity)
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/{entity}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("locations")]
    [InlineData("positions")]
    public async Task GetById_EmptyId_ShouldReturnValidationError(string entity)
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/{entity}/{Guid.Empty}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
