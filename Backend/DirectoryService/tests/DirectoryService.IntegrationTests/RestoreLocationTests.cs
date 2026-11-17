using System.Net;
using System.Net.Http.Json;
using DirectoryService.Contracts.Common;
using DirectoryService.Contracts.Locations;
using DirectoryService.Domain.Entities;
using DirectoryService.Presentation.ApiResponse;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public sealed class RestoreLocationTests : DirectoryServiceTestsBase
{
    public RestoreLocationTests(DirectoryServiceWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task RestoreLocation_WhenArchived_ShouldPreserveLinksAndRefreshBothLists()
    {
        var locationId = await SeedLocationAsync("Restorable Office");
        var departmentId = await SeedDepartmentAsync(
            "Restorable Department",
            "restorable-department",
            locationIds: [locationId]);
        using var client = CreateClient();

        var deleteResponse = await client.DeleteAsync($"/api/locations/{locationId.Value}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deletedLocation = await ExecuteDbContextAsync(dbContext => dbContext.Locations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(location => location.Id == locationId));

        var activeBefore = await client.GetFromJsonAsync<Envelope<PagedList<LocationListItemDto>>>(
            "/api/locations");
        var archivedBefore = await client.GetFromJsonAsync<Envelope<PagedList<LocationListItemDto>>>(
            "/api/locations?isArchived=true");
        activeBefore!.Result!.Items.Should().BeEmpty();
        archivedBefore!.Result!.Items.Should().ContainSingle(item => item.Id == locationId.Value);
        archivedBefore.Result.Items[0].DeletedAt.Should().Be(deletedLocation.DeletedAt);

        var response = await client.PatchAsync($"/api/locations/{locationId.Value}/restore", null);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope!.IsError.Should().BeFalse();

        await ExecuteDbContextAsync(async dbContext =>
        {
            var location = await dbContext.Locations.SingleAsync(item => item.Id == locationId);
            location.IsActive.Should().BeTrue();
            location.DeletedAt.Should().BeNull();
            location.CreatedAt.Should().Be(deletedLocation.CreatedAt);
            location.UpdatedAt.Should().BeAfter(deletedLocation.UpdatedAt);
            (await dbContext.Set<DepartmentLocation>()
                .AnyAsync(link => link.LocationId == locationId && link.DepartmentId == departmentId))
                .Should().BeTrue();
        });

        var activeAfter = await client.GetFromJsonAsync<Envelope<PagedList<LocationListItemDto>>>(
            "/api/locations");
        var archivedAfter = await client.GetFromJsonAsync<Envelope<PagedList<LocationListItemDto>>>(
            "/api/locations?isArchived=true");
        activeAfter!.Result!.Items.Should().ContainSingle(item => item.Id == locationId.Value && item.IsActive);
        activeAfter.Result.Items[0].DeletedAt.Should().BeNull();
        archivedAfter!.Result!.Items.Should().BeEmpty();

        var deleteAgain = await client.DeleteAsync($"/api/locations/{locationId.Value}");
        deleteAgain.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RestoreLocation_WhenNotFound_ShouldReturnNotFound()
    {
        using var client = CreateClient();

        var response = await client.PatchAsync($"/api/locations/{Guid.NewGuid()}/restore", null);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        envelope!.Errors.Should().ContainSingle(error => error.Code == "location.not_found");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RestoreLocation_WhenNotArchived_ShouldReturnNotFound(bool isActive)
    {
        var locationId = await SeedLocationAsync("Not Archived Office");
        if (!isActive)
        {
            await ExecuteDbContextAsync(dbContext => dbContext.Locations
                .Where(location => location.Id == locationId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(location => location.IsActive, false)));
        }

        using var client = CreateClient();
        var response = await client.PatchAsync($"/api/locations/{locationId.Value}/restore", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RestoreLocation_WhenIdIsEmpty_ShouldReturnBadRequest()
    {
        using var client = CreateClient();

        var response = await client.PatchAsync($"/api/locations/{Guid.Empty}/restore", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
