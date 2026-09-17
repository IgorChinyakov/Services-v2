using System.Net;
using System.Net.Http.Json;
using DirectoryService.Contracts.Common;
using DirectoryService.Contracts.Locations;
using DirectoryService.Presentation.ApiResponse;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public sealed class GetLocationsTests : DirectoryServiceTestsBase
{
    public static TheoryData<string> InvalidQueryParameters => new()
    {
        "page=0",
        "pageSize=0",
        "pageSize=101",
        "minDepartmentCount=-1",
        "sortBy=unknown",
        "sortDir=unknown",
        $"search={new string('a', 121)}",
    };

    private readonly DirectoryServiceWebFactory _factory;

    public GetLocationsTests(DirectoryServiceWebFactory factory)
        : base(factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetLocations_WithoutParameters_ShouldReturnDefaultPage()
    {
        // Arrange
        for (var index = 0; index < 25; index++)
            await SeedLocationAsync($"Location {index:D2}");

        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/locations");
        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<PagedList<LocationListItemDto>>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope.Should().NotBeNull();
        envelope!.IsError.Should().BeFalse();
        envelope.Result.Should().NotBeNull();
        envelope.Result!.Page.Should().Be(1);
        envelope.Result.PageSize.Should().Be(20);
        envelope.Result.TotalCount.Should().Be(25);
        envelope.Result.Items.Should().HaveCount(20);
        envelope.Result.Items.Select(location => location.Name).Should().BeInAscendingOrder();
        envelope.Result.Items.Should().OnlyContain(location => location.Address.City.Length > 0);
    }

    [Fact]
    public async Task GetLocations_WithMinimumDepartmentCount_ShouldFilterByAggregate()
    {
        // Arrange
        var quietLocationId = await SeedLocationAsync("Quiet Office");
        var busyLocationId = await SeedLocationAsync("Busy Office");
        await SeedLocationAsync("Empty Office");

        await SeedDepartmentAsync(
            "First Department",
            "firstdepartment",
            locationIds: [quietLocationId, busyLocationId]);
        await SeedDepartmentAsync(
            "Second Department",
            "seconddepartment",
            locationIds: [busyLocationId]);
        await SeedDepartmentAsync(
            "Third Department",
            "thirddepartment",
            locationIds: [busyLocationId]);

        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/locations?minDepartmentCount=2");
        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<PagedList<LocationListItemDto>>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope.Should().NotBeNull();
        envelope!.Result.Should().NotBeNull();
        envelope.Result!.TotalCount.Should().Be(1);
        envelope.Result.Items.Should().ContainSingle();
        envelope.Result.Items[0].Id.Should().Be(busyLocationId.Value);
        envelope.Result.Items[0].DepartmentCount.Should().Be(3);
    }

    [Fact]
    public async Task GetLocations_WhenLinkedDepartmentIsSoftDeleted_ShouldCountOnlyActiveDepartments()
    {
        // Arrange
        var locationId = await SeedLocationAsync("Shared office");
        var activeDepartmentId = await SeedDepartmentAsync(
            "Active Department",
            "active-department",
            locationIds: [locationId]);
        var deletedDepartmentId = await SeedDepartmentAsync(
            "Deleted Department",
            "deleted-department",
            locationIds: [locationId]);

        using var client = _factory.CreateClient();
        var deleteResponse = await client.DeleteAsync($"/api/departments/{deletedDepartmentId.Value}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var response = await client.GetAsync("/api/locations");
        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<PagedList<LocationListItemDto>>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope.Should().NotBeNull();
        envelope!.Result.Should().NotBeNull();
        envelope.Result!.Items.Should().ContainSingle(location =>
            location.Id == locationId.Value && location.DepartmentCount == 1);

        (await ExecuteDbContextAsync(dbContext => dbContext.Departments
                .AnyAsync(department => department.Id == activeDepartmentId)))
            .Should().BeTrue();
    }

    [Fact]
    public async Task GetLocations_SortByDepartmentCountDescending_ShouldReturnOrderedPage()
    {
        // Arrange
        var oneDepartmentLocationId = await SeedLocationAsync("One Department Office");
        var threeDepartmentsLocationId = await SeedLocationAsync("Three Departments Office");
        var emptyLocationId = await SeedLocationAsync("Empty Office");

        await SeedDepartmentAsync(
            "First Department",
            "firstdepartment",
            locationIds: [oneDepartmentLocationId, threeDepartmentsLocationId]);
        await SeedDepartmentAsync(
            "Second Department",
            "seconddepartment",
            locationIds: [threeDepartmentsLocationId]);
        await SeedDepartmentAsync(
            "Third Department",
            "thirddepartment",
            locationIds: [threeDepartmentsLocationId]);

        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(
            "/api/locations?sortBy=departmentCount&sortDir=desc");
        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<PagedList<LocationListItemDto>>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope.Should().NotBeNull();
        envelope!.Result.Should().NotBeNull();
        envelope.Result!.Items.Select(location => location.Id).Should().ContainInOrder(
            threeDepartmentsLocationId.Value,
            oneDepartmentLocationId.Value,
            emptyLocationId.Value);
        envelope.Result.Items.Select(location => location.DepartmentCount).Should().ContainInOrder(3, 1, 0);
    }

    [Fact]
    public async Task GetLocations_SortByCreatedAtDescending_ShouldReturnNewestFirst()
    {
        // Arrange
        var olderLocationId = await SeedLocationAsync("Older Office");
        var newerLocationId = await SeedLocationAsync("Newer Office");

        await ExecuteDbContextAsync(async dbContext =>
        {
            await dbContext.Locations
                .Where(location => location.Id == olderLocationId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    location => location.CreatedAt,
                    DateTime.UtcNow.AddDays(-2)));

            await dbContext.Locations
                .Where(location => location.Id == newerLocationId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    location => location.CreatedAt,
                    DateTime.UtcNow.AddDays(-1)));
        });

        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(
            "/api/locations?sortBy=createdAt&sortDir=desc");
        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<PagedList<LocationListItemDto>>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope.Should().NotBeNull();
        envelope!.Result.Should().NotBeNull();
        envelope.Result!.Items.Select(location => location.Id).Should().ContainInOrder(
            newerLocationId.Value,
            olderLocationId.Value);
    }

    [Fact]
    public async Task GetLocations_Search_ShouldBeCaseInsensitive()
    {
        // Arrange
        await SeedLocationAsync("Central Moscow Office");
        await SeedLocationAsync("Unrelated Warehouse");
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/locations?search=mOsCoW");
        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<PagedList<LocationListItemDto>>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope.Should().NotBeNull();
        envelope!.Result.Should().NotBeNull();
        envelope.Result!.TotalCount.Should().Be(1);
        envelope.Result.Items.Should().ContainSingle(location => location.Name == "Central Moscow Office");
    }

    [Fact]
    public async Task GetLocations_WhenLocationIsSoftDeleted_ShouldNotReturnDeletedLocation()
    {
        // Arrange
        var activeLocationId = await SeedLocationAsync("Active office");
        var deletedLocationId = await SeedLocationAsync("Deleted office");

        await ExecuteDbContextAsync(async dbContext =>
        {
            await dbContext.Locations
                .Where(location => location.Id == deletedLocationId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(location => location.IsActive, false)
                    .SetProperty(location => location.DeletedAt, DateTime.UtcNow));
        });

        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/locations");
        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<PagedList<LocationListItemDto>>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope.Should().NotBeNull();
        envelope!.Result.Should().NotBeNull();
        envelope.Result!.Items.Should().ContainSingle(location => location.Id == activeLocationId.Value);
        envelope.Result.Items.Should().NotContain(location => location.Id == deletedLocationId.Value);
        envelope.Result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetLocations_PageWithoutItems_ShouldPreserveTotalCount()
    {
        // Arrange
        await SeedLocationAsync("First Office");
        await SeedLocationAsync("Second Office");
        await SeedLocationAsync("Third Office");
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/locations?page=3&pageSize=2");
        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<PagedList<LocationListItemDto>>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope.Should().NotBeNull();
        envelope!.Result.Should().NotBeNull();
        envelope.Result!.Items.Should().BeEmpty();
        envelope.Result.TotalCount.Should().Be(3);
    }

    [Theory]
    [MemberData(nameof(InvalidQueryParameters))]
    public async Task GetLocations_WithInvalidParameters_ShouldReturnValidationError(
        string queryString)
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/locations?{queryString}");
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        envelope.Should().NotBeNull();
        envelope!.IsError.Should().BeTrue();
        envelope.Errors.Should().NotBeEmpty();
    }
}
