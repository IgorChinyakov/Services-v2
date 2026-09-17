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
        var rootsResponse = await client.GetAsync("/api/departments/roots");
        var childrenResponse = await client.GetAsync(
            $"/api/departments/{activeRootId.Value}/children");

        var listEnvelope = await listResponse.Content
            .ReadFromJsonAsync<Envelope<PagedList<DepartmentListItemDto>>>();
        var rootsEnvelope = await rootsResponse.Content
            .ReadFromJsonAsync<Envelope<PagedList<RootDepartmentDto>>>();
        var childrenEnvelope = await childrenResponse.Content
            .ReadFromJsonAsync<Envelope<PagedList<DepartmentNodeDto>>>();

        // Assert
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        rootsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        childrenResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        listEnvelope!.Result!.Items.Should().ContainSingle(item => item.Id == activeRootId.Value);
        listEnvelope.Result.Items.Should().NotContain(item =>
            item.Id == deletedRootId.Value || item.Id == deletedChildId.Value);

        rootsEnvelope!.Result!.Items.Should().ContainSingle(item => item.Id == activeRootId.Value);
        rootsEnvelope.Result.Items.Should().NotContain(item => item.Id == deletedRootId.Value);

        childrenEnvelope!.Result!.Items.Should().BeEmpty();
        childrenEnvelope.Result.TotalCount.Should().Be(0);
    }
}
