using System.Net;
using System.Net.Http.Json;
using DirectoryService.Contracts.Departments;
using DirectoryService.Presentation.ApiResponse;
using FluentAssertions;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public sealed class DepartmentTreeTests : DirectoryServiceTestsBase
{
    public DepartmentTreeTests(DirectoryServiceWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetTree_WhenDepartmentsDoNotExist_ShouldReturnEmptyList()
    {
        // Arrange
        using var client = CreateClient();

        // Act
        var response = await client.GetAsync("/api/departments/tree");
        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<IReadOnlyList<DepartmentTreeNodeDto>>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope.Should().NotBeNull();
        envelope!.IsError.Should().BeFalse();
        envelope.Result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTree_ShouldReturnOnlyRootsWithHasChildrenFlag()
    {
        // Arrange
        var headId = await SeedDepartmentAsync("Head Office", "head");
        var branchId = await SeedDepartmentAsync("Branch Office", "branch");
        await SeedDepartmentAsync("Development Department", "development", headId);
        using var client = CreateClient();

        // Act
        var response = await client.GetAsync("/api/departments/tree");
        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<IReadOnlyList<DepartmentTreeNodeDto>>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope!.Result.Should().HaveCount(2);
        envelope.Result.Should().ContainSingle(node =>
            node.Id == headId.Value && node.Depth == 0 && node.HasChildren);
        envelope.Result.Should().ContainSingle(node =>
            node.Id == branchId.Value && node.Depth == 0 && !node.HasChildren);
        envelope.Result.Should().OnlyContain(node => node.Path == node.Slug);
    }

    [Fact]
    public async Task GetChildren_ShouldReturnOnlyDirectChildrenWithHasChildrenFlag()
    {
        // Arrange
        var rootId = await SeedDepartmentAsync("Head Office", "head");
        var childId = await SeedDepartmentAsync("IT Department", "itdepartment", rootId);
        var secondChildId = await SeedDepartmentAsync("Sales Department", "sales", rootId);
        await SeedDepartmentAsync("Development Team", "development", childId);
        using var client = CreateClient();

        // Act
        var response = await client.GetAsync($"/api/departments/{rootId.Value}/children");
        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<IReadOnlyList<DepartmentTreeNodeDto>>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope!.Result.Should().HaveCount(2);
        envelope.Result.Should().ContainSingle(node =>
            node.Id == childId.Value && node.Depth == 1 && node.HasChildren);
        envelope.Result.Should().ContainSingle(node =>
            node.Id == secondChildId.Value && node.Depth == 1 && !node.HasChildren);
        envelope.Result.Should().OnlyContain(node => node.Path.StartsWith("head.", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetChildren_WhenDepartmentIsLeaf_ShouldReturnEmptyList()
    {
        // Arrange
        var departmentId = await SeedDepartmentAsync("Head Office", "head");
        using var client = CreateClient();

        // Act
        var response = await client.GetAsync($"/api/departments/{departmentId.Value}/children");
        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<IReadOnlyList<DepartmentTreeNodeDto>>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope!.Result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetChildren_WhenDepartmentDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        using var client = CreateClient();

        // Act
        var response = await client.GetAsync($"/api/departments/{Guid.NewGuid()}/children");
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        envelope.Should().NotBeNull();
        envelope!.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task GetAncestors_ShouldReturnRootToDirectParentAndExcludeTarget()
    {
        // Arrange
        var rootId = await SeedDepartmentAsync("Head Office", "head");
        var parentId = await SeedDepartmentAsync("IT Department", "itdepartment", rootId);
        var targetId = await SeedDepartmentAsync("Development Team", "development", parentId);
        using var client = CreateClient();

        // Act
        var response = await client.GetAsync($"/api/departments/{targetId.Value}/ancestors");
        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<IReadOnlyList<DepartmentTreeNodeDto>>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope!.Result!.Select(node => node.Id).Should().ContainInOrder(
            rootId.Value,
            parentId.Value);
        envelope.Result.Should().NotContain(node => node.Id == targetId.Value);
    }

    [Fact]
    public async Task GetAncestors_WhenDepartmentIsRoot_ShouldReturnEmptyList()
    {
        // Arrange
        var rootId = await SeedDepartmentAsync("Head Office", "head");
        using var client = CreateClient();

        // Act
        var response = await client.GetAsync($"/api/departments/{rootId.Value}/ancestors");
        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<IReadOnlyList<DepartmentTreeNodeDto>>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope!.Result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAncestors_WhenDepartmentDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        using var client = CreateClient();

        // Act
        var response = await client.GetAsync($"/api/departments/{Guid.NewGuid()}/ancestors");
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        envelope.Should().NotBeNull();
        envelope!.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task SearchTree_ShouldBeCaseInsensitiveAndReturnAncestors()
    {
        // Arrange
        var rootId = await SeedDepartmentAsync("Head Office", "head");
        var parentId = await SeedDepartmentAsync("Sales Department", "sales", rootId);
        var targetId = await SeedDepartmentAsync("Regional Sales", "regional", parentId);
        await SeedDepartmentAsync("Development Department", "development", rootId);
        using var client = CreateClient();

        // Act
        var response = await client.GetAsync("/api/departments/tree/search?q=rEgIoNaL");
        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<IReadOnlyList<DepartmentTreeSearchResultDto>>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = envelope!.Result.Should().ContainSingle().Subject;
        result.Department.Id.Should().Be(targetId.Value);
        result.Ancestors.Select(node => node.Id).Should().ContainInOrder(
            rootId.Value,
            parentId.Value);
    }

    [Fact]
    public async Task SearchTree_WhenNothingMatches_ShouldReturnEmptyList()
    {
        // Arrange
        await SeedDepartmentAsync("Head Office", "head");
        using var client = CreateClient();

        // Act
        var response = await client.GetAsync("/api/departments/tree/search?q=unknown");
        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<IReadOnlyList<DepartmentTreeSearchResultDto>>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        envelope!.Result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchTree_WhenQueryIsTooShort_ShouldReturnBadRequest()
    {
        // Arrange
        using var client = CreateClient();

        // Act
        var response = await client.GetAsync("/api/departments/tree/search?q=a");
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        envelope.Should().NotBeNull();
        envelope!.IsError.Should().BeTrue();
    }
}
