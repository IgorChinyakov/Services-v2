using System.Net;
using System.Net.Http.Json;
using DirectoryService.Contracts.Common;
using DirectoryService.Contracts.Departments;
using DirectoryService.Presentation.ApiResponse;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public sealed class GetDepartmentsTests : DirectoryServiceTestsBase
{
    public static TheoryData<string> InvalidQueryParameters => new()
    {
        "page=0",
        "pageSize=0",
        "pageSize=101",
        "sortBy=unknown",
        "sortDirection=unknown",
        $"parentId={Guid.Empty}",
        $"locationIds={Guid.Empty}",
        $"excludeIds={Guid.Empty}",
        $"search={new string('a', 151)}",
    };

    private readonly DirectoryServiceWebFactory _factory;

    public GetDepartmentsTests(DirectoryServiceWebFactory factory)
        : base(factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetDepartments_WithoutParameters_ShouldReturnFirstPageWithDefaults()
    {
        // Arrange
        for (var index = 0; index < 25; index++)
        {
            await SeedDepartmentAsync(
                $"Department {index:D2}",
                $"department{GetAlphabeticSuffix(index)}");
        }

        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/departments");
        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<PagedList<DepartmentListItemDto>>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope.Should().NotBeNull();
        envelope!.IsError.Should().BeFalse();
        envelope.Result.Should().NotBeNull();
        envelope.Result!.Page.Should().Be(1);
        envelope.Result.PageSize.Should().Be(20);
        envelope.Result.TotalCount.Should().Be(25);
        envelope.Result.Items.Should().HaveCount(20);
        envelope.Result.Items.Select(item => item.Name).Should().BeInAscendingOrder();
        envelope.Result.Items.Should().OnlyContain(item =>
            item.IsActive && item.UpdatedAt >= item.CreatedAt && item.DeletedAt == null);
    }

    [Fact]
    public async Task GetDepartments_SecondPage_ShouldReturnRemainingItemsAndPreserveTotalCount()
    {
        // Arrange
        for (var index = 0; index < 25; index++)
        {
            await SeedDepartmentAsync(
                $"Department {index:D2}",
                $"department{GetAlphabeticSuffix(index)}");
        }

        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/departments?page=2&pageSize=20");
        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<PagedList<DepartmentListItemDto>>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope.Should().NotBeNull();
        envelope!.Result.Should().NotBeNull();
        envelope.Result!.Page.Should().Be(2);
        envelope.Result.PageSize.Should().Be(20);
        envelope.Result.TotalCount.Should().Be(25);
        envelope.Result.Items.Should().HaveCount(5);
    }

    [Theory]
    [InlineData("Sales Department", "sAlEs")]
    [InlineData("Отдел Продаж", "пРоДаЖ")]
    public async Task GetDepartments_Search_ShouldBeCaseInsensitive(
        string departmentName,
        string search)
    {
        // Arrange
        await SeedDepartmentAsync(departmentName, "searchdepartment");
        await SeedDepartmentAsync("Unrelated Department", "unrelateddepartment");
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/departments?search={search}");
        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<PagedList<DepartmentListItemDto>>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope.Should().NotBeNull();
        envelope!.Result.Should().NotBeNull();
        envelope.Result!.TotalCount.Should().Be(1);
        envelope.Result.Items.Should().ContainSingle(item => item.Name == departmentName);
    }

    [Fact]
    public async Task GetDepartments_SortByNameDescending_ShouldReturnOrderedPage()
    {
        // Arrange
        await SeedDepartmentAsync("Alpha Department", "alphadepartment");
        await SeedDepartmentAsync("Charlie Department", "charliedepartment");
        await SeedDepartmentAsync("Bravo Department", "bravodepartment");
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(
            "/api/departments?sortBy=name&sortDirection=desc");
        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<PagedList<DepartmentListItemDto>>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope.Should().NotBeNull();
        envelope!.Result.Should().NotBeNull();
        envelope.Result!.Items.Select(item => item.Name).Should().ContainInOrder(
            "Charlie Department",
            "Bravo Department",
            "Alpha Department");
    }

    [Fact]
    public async Task GetDepartments_SortByCreatedAtDescending_ShouldReturnNewestFirst()
    {
        // Arrange
        var olderDepartmentId = await SeedDepartmentAsync("Older Department", "olderdepartment");
        var newerDepartmentId = await SeedDepartmentAsync("Newer Department", "newerdepartment");

        await ExecuteDbContextAsync(async dbContext =>
        {
            await dbContext.Departments
                .Where(department => department.Id == olderDepartmentId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(
                        department => department.CreatedAt,
                        DateTime.UtcNow.AddDays(-2)));

            await dbContext.Departments
                .Where(department => department.Id == newerDepartmentId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(
                        department => department.CreatedAt,
                        DateTime.UtcNow.AddDays(-1)));
        });

        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(
            "/api/departments?sortBy=createdAt&sortDirection=desc");
        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<PagedList<DepartmentListItemDto>>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope.Should().NotBeNull();
        envelope!.Result.Should().NotBeNull();
        envelope.Result!.Items.Select(item => item.Id).Should().ContainInOrder(
            newerDepartmentId.Value,
            olderDepartmentId.Value);
    }

    [Fact]
    public async Task GetDepartments_IsActiveFalse_ShouldReturnOnlyInactiveDepartments()
    {
        var activeId = await SeedDepartmentAsync("Active Department", "activedepartment");
        var locationId = await SeedLocationAsync("Inactive Department Location");
        var inactiveId = await SeedDepartmentAsync(
            "Inactive Department", "inactivedepartment", locationIds: [locationId]);
        var deletedAt = DateTime.UtcNow;

        await ExecuteDbContextAsync(dbContext => dbContext.Departments
            .Where(department => department.Id == inactiveId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(department => department.IsActive, false)
                .SetProperty(department => department.UpdatedAt, deletedAt)
                .SetProperty(department => department.DeletedAt, deletedAt)));

        using var client = _factory.CreateClient();

        var defaultResponse = await client.GetFromJsonAsync<Envelope<PagedList<DepartmentListItemDto>>>(
            "/api/departments");
        var inactiveResponse = await client.GetFromJsonAsync<Envelope<PagedList<DepartmentListItemDto>>>(
            "/api/departments?isActive=false");
        var inactiveAtLocationResponse = await client
            .GetFromJsonAsync<Envelope<PagedList<DepartmentListItemDto>>>(
                $"/api/departments?isActive=false&locationIds={locationId.Value}");

        defaultResponse!.Result!.Items.Should().ContainSingle(item => item.Id == activeId.Value);
        inactiveResponse!.Result!.TotalCount.Should().Be(1);
        inactiveResponse.Result.Items.Should().ContainSingle(item => item.Id == inactiveId.Value);
        inactiveResponse.Result.Items[0].IsActive.Should().BeFalse();
        inactiveResponse.Result.Items[0].UpdatedAt.Should().Be(deletedAt);
        inactiveResponse.Result.Items[0].DeletedAt.Should().Be(deletedAt);
        inactiveAtLocationResponse!.Result!.Items.Should().ContainSingle(item => item.Id == inactiveId.Value);
    }

    [Fact]
    public async Task GetDepartments_ParentId_ShouldReturnDirectChildrenOnly()
    {
        var parentId = await SeedDepartmentAsync("Parent Department", "parentdepartment");
        var childId = await SeedDepartmentAsync("Child Department", "childdepartment", parentId);
        await SeedDepartmentAsync("Grandchild Department", "grandchilddepartment", childId);

        using var client = _factory.CreateClient();
        var response = await client.GetFromJsonAsync<Envelope<PagedList<DepartmentListItemDto>>>(
            $"/api/departments?parentId={parentId.Value}");

        response!.Result!.TotalCount.Should().Be(1);
        response.Result.Items.Should().ContainSingle(item => item.Id == childId.Value);
    }

    [Fact]
    public async Task GetDepartments_LocationIds_ShouldMatchAnyActiveLocation()
    {
        var firstLocationId = await SeedLocationAsync("First Location");
        var secondLocationId = await SeedLocationAsync("Second Location");
        var deletedLocationId = await SeedLocationAsync("Deleted Location");
        var firstDepartmentId = await SeedDepartmentAsync(
            "First Department", "firstdepartment", locationIds: [firstLocationId]);
        var secondDepartmentId = await SeedDepartmentAsync(
            "Second Department", "seconddepartment", locationIds: [secondLocationId]);
        await SeedDepartmentAsync(
            "Deleted Location Department", "deletedlocationdepartment", locationIds: [deletedLocationId]);

        await ExecuteDbContextAsync(dbContext => dbContext.Locations
            .Where(location => location.Id == deletedLocationId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(location => location.IsActive, false)));

        using var client = _factory.CreateClient();
        var response = await client.GetFromJsonAsync<Envelope<PagedList<DepartmentListItemDto>>>(
            $"/api/departments?locationIds={firstLocationId.Value}" +
            $"&locationIds={secondLocationId.Value}&locationIds={deletedLocationId.Value}");

        response!.Result!.TotalCount.Should().Be(2);
        response.Result.Items.Select(item => item.Id).Should().BeEquivalentTo(
            [firstDepartmentId.Value, secondDepartmentId.Value]);
    }

    [Fact]
    public async Task GetDepartments_ExcludeIds_ShouldExcludeAllSpecifiedDepartments()
    {
        var firstId = await SeedDepartmentAsync("First Department", "firstdepartment");
        var secondId = await SeedDepartmentAsync("Second Department", "seconddepartment");
        var remainingId = await SeedDepartmentAsync("Remaining Department", "remainingdepartment");

        using var client = _factory.CreateClient();
        var response = await client.GetFromJsonAsync<Envelope<PagedList<DepartmentListItemDto>>>(
            $"/api/departments?excludeIds={firstId.Value}&excludeIds={secondId.Value}");

        response!.Result!.TotalCount.Should().Be(1);
        response.Result.Items.Should().ContainSingle(item => item.Id == remainingId.Value);
    }

    [Fact]
    public async Task GetDepartments_CombinedFilters_ShouldApplyBeforePagingAndUseDistinctCacheKeys()
    {
        var locationId = await SeedLocationAsync("Shared Location");
        var parentId = await SeedDepartmentAsync("Parent Department", "parentdepartment");
        var excludedId = await SeedDepartmentAsync(
            "Excluded Department", "excludeddepartment", parentId, [locationId]);
        var includedId = await SeedDepartmentAsync(
            "Included Department", "includeddepartment", parentId, [locationId]);

        using var client = _factory.CreateClient();
        var unfiltered = await client.GetFromJsonAsync<Envelope<PagedList<DepartmentListItemDto>>>(
            "/api/departments?pageSize=1");
        var filtered = await client.GetFromJsonAsync<Envelope<PagedList<DepartmentListItemDto>>>(
            $"/api/departments?parentId={parentId.Value}&locationIds={locationId.Value}" +
            $"&excludeIds={excludedId.Value}&pageSize=1");

        unfiltered!.Result!.TotalCount.Should().Be(3);
        filtered!.Result!.TotalCount.Should().Be(1);
        filtered.Result.Items.Should().ContainSingle(item => item.Id == includedId.Value);
    }

    [Theory]
    [MemberData(nameof(InvalidQueryParameters))]
    public async Task GetDepartments_WithInvalidParameters_ShouldReturnValidationError(
        string queryString)
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/departments?{queryString}");
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        envelope.Should().NotBeNull();
        envelope!.IsError.Should().BeTrue();
        envelope.Errors.Should().NotBeEmpty();
    }

    private static string GetAlphabeticSuffix(int index)
    {
        return ((char)('a' + index)).ToString();
    }
}
