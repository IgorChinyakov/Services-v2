using System.Net;
using System.Net.Http.Json;
using DirectoryService.Contracts.Common;
using DirectoryService.Contracts.Departments;
using DirectoryService.Presentation.ApiResponse;
using FluentAssertions;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public sealed class SoftDeleteReadVisibilityTests : DirectoryServiceTestsBase
{
    public SoftDeleteReadVisibilityTests(DirectoryServiceWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task DepartmentReads_WhenRootAndChildAreSoftDeleted_ShouldHideThemFromListAndTree()
    {
        // Arrange
        var activeRootId = await SeedDepartmentAsync("Active Root", "active-root");
        var deletedRootId = await SeedDepartmentAsync("Deleted Root", "deleted-root");
        var deletedChildId = await SeedDepartmentAsync(
            "Deleted Child",
            "deleted-child",
            activeRootId);

        using var client = CreateClient();
        (await client.DeleteAsync($"/api/departments/{deletedRootId.Value}"))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.DeleteAsync($"/api/departments/{deletedChildId.Value}"))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var listResponse = await client.GetAsync("/api/departments");
        var treeResponse = await client.GetAsync("/api/departments/tree");
        var childrenResponse = await client.GetAsync(
            $"/api/departments/{activeRootId.Value}/children");
        var ancestorsResponse = await client.GetAsync(
            $"/api/departments/{deletedChildId.Value}/ancestors");
        var searchResponse = await client.GetAsync(
            "/api/departments/tree/search?q=deleted");

        var listEnvelope = await listResponse.Content
            .ReadFromJsonAsync<Envelope<PagedList<DepartmentListItemDto>>>();
        var treeEnvelope = await treeResponse.Content
            .ReadFromJsonAsync<Envelope<IReadOnlyList<DepartmentTreeNodeDto>>>();
        var childrenEnvelope = await childrenResponse.Content
            .ReadFromJsonAsync<Envelope<IReadOnlyList<DepartmentTreeNodeDto>>>();
        var searchEnvelope = await searchResponse.Content
            .ReadFromJsonAsync<Envelope<IReadOnlyList<DepartmentTreeSearchResultDto>>>();

        // Assert
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        treeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        childrenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        ancestorsResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        listEnvelope!.Result!.Items.Should().ContainSingle(item => item.Id == activeRootId.Value);
        listEnvelope.Result.Items.Should().NotContain(item =>
            item.Id == deletedRootId.Value || item.Id == deletedChildId.Value);

        treeEnvelope!.Result.Should().ContainSingle(item => item.Id == activeRootId.Value);
        treeEnvelope.Result.Should().NotContain(item => item.Id == deletedRootId.Value);

        childrenEnvelope!.Result.Should().BeEmpty();
        searchEnvelope!.Result.Should().BeEmpty();
    }
}
