using System.Net;
using System.Net.Http.Json;
using DirectoryService.Contracts.Common;
using DirectoryService.Contracts.Departments;
using DirectoryService.Contracts.Departments.Requests;
using DirectoryService.Contracts.Positions.Requests;
using DirectoryService.Presentation.ApiResponse;
using FluentAssertions;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public sealed class DepartmentCachingTests : DirectoryServiceTestsBase
{
    public DepartmentCachingTests(DirectoryServiceWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetTree_WhenCacheIsEmpty_ShouldStoreResultInRedis()
    {
        // Arrange
        await SeedDepartmentAsync("Head Office", "head");
        using var client = CreateClient();
        var keysBeforeQuery = await GetRedisDatabaseSizeAsync();

        // Act
        var response = await client.GetAsync(TreeEndpoint);
        var keysAfterQuery = await WaitForRedisWriteAsync(keysBeforeQuery);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        keysAfterQuery.Should().BeGreaterThan(keysBeforeQuery);
    }

    [Fact]
    public async Task GetTree_WhenSameRequestIsRepeated_ShouldReturnCachedResult()
    {
        // Arrange
        await SeedDepartmentAsync("Head Office", "head");
        using var client = CreateClient();
        var firstResponse = await client.GetAsync(TreeEndpoint);
        firstResponse.EnsureSuccessStatusCode();
        await SeedDepartmentAsync("Sales Department", "sales");

        // Act
        var secondResult = await GetTreeAsync(client);

        // Assert
        secondResult.Select(department => department.Slug).Should().Equal("head");
    }

    [Fact]
    public async Task CreateDepartment_WhenRootsAreCached_ShouldInvalidateCache()
    {
        // Arrange
        await SeedDepartmentAsync("Head Office", "head");
        var locationId = await SeedLocationAsync();
        using var client = CreateClient();
        (await client.GetAsync(TreeEndpoint)).EnsureSuccessStatusCode();
        var request = new CreateDepartmentRequest
        {
            Name = "Sales Department",
            Slug = "sales",
            LocationIds = [locationId.Value],
        };

        // Act
        var commandResponse = await client.PostAsJsonAsync("/api/departments", request);
        var queryResult = await GetTreeAsync(client);

        // Assert
        commandResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        queryResult.Select(department => department.Slug)
            .Should().BeEquivalentTo(["head", "sales"]);
    }

    [Fact]
    public async Task UpdateParent_WhenChildrenAreCached_ShouldInvalidateBothParentQueries()
    {
        // Arrange
        var firstParentId = await SeedDepartmentAsync("Head Office", "head");
        var secondParentId = await SeedDepartmentAsync("Sales Department", "sales");
        var childId = await SeedDepartmentAsync("Development Department", "development", firstParentId);
        using var client = CreateClient();
        var firstEndpoint = ChildrenEndpoint(firstParentId.Value);
        var secondEndpoint = ChildrenEndpoint(secondParentId.Value);
        (await client.GetAsync(firstEndpoint)).EnsureSuccessStatusCode();
        (await client.GetAsync(secondEndpoint)).EnsureSuccessStatusCode();

        // Act
        var commandResponse = await client.PutAsJsonAsync(
            $"/api/departments/{childId.Value}/parent",
            new UpdateDepartmentParentRequest { ParentId = secondParentId.Value });
        var firstParentResult = await GetChildrenAsync(client, firstEndpoint);
        var secondParentResult = await GetChildrenAsync(client, secondEndpoint);

        // Assert
        commandResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstParentResult.Should().BeEmpty();
        secondParentResult.Should().ContainSingle(department => department.Id == childId.Value);
    }

    [Fact]
    public async Task SoftDeleteDepartment_WhenRootsAreCached_ShouldInvalidateCache()
    {
        // Arrange
        await SeedDepartmentAsync("Head Office", "head");
        var departmentToDeleteId = await SeedDepartmentAsync("Sales Department", "sales");
        using var client = CreateClient();
        (await client.GetAsync(TreeEndpoint)).EnsureSuccessStatusCode();

        // Act
        var commandResponse = await client.DeleteAsync($"/api/departments/{departmentToDeleteId.Value}");
        var queryResult = await GetTreeAsync(client);

        // Assert
        commandResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        queryResult.Select(department => department.Slug).Should().Equal("head");
    }

    [Fact]
    public async Task CreatePosition_WhenTopDepartmentsAreCached_ShouldInvalidateCache()
    {
        // Arrange
        var departmentId = await SeedDepartmentAsync("Development Department", "development");
        using var client = CreateClient();
        var cachedResult = await GetTopDepartmentsAsync(client);
        cachedResult.Should().ContainSingle(department =>
            department.Id == departmentId.Value && department.PositionsCount == 0);

        // Act
        var commandResponse = await client.PostAsJsonAsync(
            "/api/positions",
            new CreatePositionRequest
            {
                Name = "Software Developer",
                DepartmentIds = [departmentId.Value],
            });
        var queryResult = await GetTopDepartmentsAsync(client);

        // Assert
        commandResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        queryResult.Should().ContainSingle(department =>
            department.Id == departmentId.Value && department.PositionsCount == 1);
    }

    private const string TreeEndpoint = "/api/departments/tree";

    private static string ChildrenEndpoint(Guid parentId) =>
        $"/api/departments/{parentId}/children";

    private static async Task<IReadOnlyList<DepartmentTreeNodeDto>> GetTreeAsync(HttpClient client)
    {
        var envelope = await client.GetFromJsonAsync<Envelope<IReadOnlyList<DepartmentTreeNodeDto>>>(
            TreeEndpoint);
        return envelope!.Result!;
    }

    private static async Task<IReadOnlyList<DepartmentTreeNodeDto>> GetChildrenAsync(
        HttpClient client,
        string endpoint)
    {
        var envelope = await client.GetFromJsonAsync<Envelope<IReadOnlyList<DepartmentTreeNodeDto>>>(
            endpoint);
        return envelope!.Result!;
    }

    private static async Task<IReadOnlyList<TopDepartmentByPositionsDto>> GetTopDepartmentsAsync(
        HttpClient client)
    {
        var envelope = await client.GetFromJsonAsync<Envelope<IReadOnlyList<TopDepartmentByPositionsDto>>>(
            "/api/departments/top-positions");
        return envelope!.Result!;
    }

    private async Task<long> WaitForRedisWriteAsync(long initialSize)
    {
        const int maxAttempts = 20;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var currentSize = await GetRedisDatabaseSizeAsync();
            if (currentSize > initialSize)
                return currentSize;

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }

        return await GetRedisDatabaseSizeAsync();
    }
}
