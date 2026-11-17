using System.Net;
using System.Net.Http.Json;
using DirectoryService.Contracts.Common;
using DirectoryService.Contracts.Departments;
using DirectoryService.Contracts.Departments.Requests;
using DirectoryService.Domain.Entities;
using DirectoryService.Presentation.ApiResponse;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public sealed class UpdateDepartmentActivityTests(DirectoryServiceWebFactory factory)
    : DirectoryServiceTestsBase(factory)
{
    [Fact]
    public async Task ChangeActivity_ShouldPreserveStructureAndLinksAndInvalidateCachedReads()
    {
        var locationId = await SeedLocationAsync("Activity Office");
        var id = await SeedDepartmentAsync("Activity Root", "activityroot", locationIds: [locationId]);
        var positionId = await SeedPositionAsync("Activity Position", [id]);
        using var client = CreateClient();
        (await ReadList(client, "?isArchived=false")).Items.Should().ContainSingle(item => item.IsActive);
        (await ReadTree(client)).Should().ContainSingle(item => item.IsActive);

        (await client.PatchAsJsonAsync(
            $"/api/departments/{id.Value}/activity",
            new UpdateDepartmentActivityRequest { IsActive = false })).EnsureSuccessStatusCode();

        var all = await ReadList(client, "?isArchived=false");
        all.Items.Should().ContainSingle(item => item.Id == id.Value && !item.IsActive && item.DeletedAt == null);
        (await ReadList(client, "?isArchived=false&isActive=true")).Items.Should().BeEmpty();
        (await ReadList(client, "?isArchived=false&isActive=false")).Items.Should().ContainSingle();
        (await ReadList(client, "?isArchived=false")).Items.Should().ContainSingle();
        (await ReadList(client, "?isArchived=true")).Items.Should().BeEmpty();
        (await ReadTree(client)).Should().ContainSingle(item => item.Id == id.Value && !item.IsActive);
        var details = await client.GetFromJsonAsync<Envelope<DepartmentDetailsDto>>($"/api/departments/{id.Value}");
        details!.Result!.IsActive.Should().BeFalse();

        await ExecuteDbContextAsync(async db =>
        {
            var department = await db.Departments.IgnoreQueryFilters().SingleAsync(item => item.Id == id);
            department.Path.Should().Be("activityroot");
            department.ParentId.Should().BeNull();
            department.DeletedAt.Should().BeNull();
            (await db.Set<DepartmentLocation>().IgnoreQueryFilters()
                .AnyAsync(item => item.DepartmentId == id && item.LocationId == locationId)).Should().BeTrue();
            (await db.Set<DepartmentPosition>().IgnoreQueryFilters()
                .AnyAsync(item => item.DepartmentId == id && item.PositionId == positionId)).Should().BeTrue();
            (await db.Locations.SingleAsync(item => item.Id == locationId)).IsActive.Should().BeTrue();
            (await db.Positions.SingleAsync(item => item.Id == positionId)).IsActive.Should().BeTrue();
        });

        (await client.PatchAsJsonAsync(
            $"/api/departments/{id.Value}/activity",
            new { isActive = true })).EnsureSuccessStatusCode();
        (await ReadTree(client)).Should().ContainSingle(item => item.IsActive);
        (await ReadList(client, "?isArchived=false&isActive=true")).Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Deactivate_WithActiveDescendant_ShouldRejectWithoutChanges()
    {
        var root = await SeedDepartmentAsync("Root", "activeroot");
        var child = await SeedDepartmentAsync("Child", "activechild", root);
        using var client = CreateClient();
        var response = await client.PatchAsJsonAsync($"/api/departments/{root.Value}/activity", new { isActive = false });
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadFromJsonAsync<Envelope>())!.Errors.Should()
            .ContainSingle(item => item.Code == "department.activity.active_descendants");
        (await ReadTree(client)).Should().ContainSingle(item => item.IsActive && item.HasChildren);

        (await client.PatchAsJsonAsync($"/api/departments/{child.Value}/activity", new { isActive = false }))
            .EnsureSuccessStatusCode();
        (await client.PatchAsJsonAsync($"/api/departments/{root.Value}/activity", new { isActive = false }))
            .EnsureSuccessStatusCode();
        var children = await client.GetFromJsonAsync<Envelope<IReadOnlyList<DepartmentTreeNodeDto>>>(
            $"/api/departments/{root.Value}/children");
        children!.Result.Should().ContainSingle(item => item.Id == child.Value && !item.IsActive);
        (await ReadTree(client)).Should().ContainSingle(item => !item.IsActive && item.HasChildren);

        var activation = await client.PatchAsJsonAsync($"/api/departments/{child.Value}/activity", new { isActive = true });
        activation.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await activation.Content.ReadFromJsonAsync<Envelope>())!.Errors.Should()
            .ContainSingle(item => item.Code == "department.activity.parent_inactive");
    }

    [Fact]
    public async Task InactiveTree_ShouldSupportSearchAndAncestors()
    {
        var root = await SeedDepartmentAsync("Activity Root", "searchactivityroot");
        var child = await SeedDepartmentAsync("Activity Child", "searchactivitychild", root);
        using var client = CreateClient();
        (await client.PatchAsJsonAsync($"/api/departments/{child.Value}/activity", new { isActive = false })).EnsureSuccessStatusCode();
        (await client.PatchAsJsonAsync($"/api/departments/{root.Value}/activity", new { isActive = false })).EnsureSuccessStatusCode();
        var ancestors = await client.GetFromJsonAsync<Envelope<IReadOnlyList<DepartmentTreeNodeDto>>>(
            $"/api/departments/{child.Value}/ancestors");
        ancestors!.Result.Should().ContainSingle(item => item.Id == root.Value && !item.IsActive);
        var search = await client.GetFromJsonAsync<Envelope<IReadOnlyList<DepartmentTreeSearchResultDto>>>(
            "/api/departments/tree/search?q=Activity");
        search!.Result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Activity_ShouldBeIdempotentAndNotRestoreArchivedDepartments()
    {
        var id = await SeedDepartmentAsync("Activity Leaf", "activityleaf");
        using var client = CreateClient();
        for (var index = 0; index < 2; index++)
        {
            (await client.PatchAsJsonAsync($"/api/departments/{id.Value}/activity", new { isActive = false }))
                .EnsureSuccessStatusCode();
        }

        (await client.DeleteAsync($"/api/departments/{id.Value}")).EnsureSuccessStatusCode();
        var response = await client.PatchAsJsonAsync($"/api/departments/{id.Value}/activity", new { isActive = true });
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadFromJsonAsync<Envelope>())!.Errors.Should()
            .ContainSingle(item => item.Code == "department.activity.archived");
        (await ReadList(client, "?isArchived=true")).Items.Should().ContainSingle(item => item.DeletedAt != null);
        (await ReadTree(client)).Should().BeEmpty();
    }

    [Fact]
    public async Task Activity_ShouldValidateBodyAndId()
    {
        using var client = CreateClient();
        (await client.PatchAsJsonAsync($"/api/departments/{Guid.NewGuid()}/activity", new { }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.PatchAsJsonAsync($"/api/departments/{Guid.Empty}/activity", new { isActive = false }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.PatchAsJsonAsync($"/api/departments/{Guid.NewGuid()}/activity", new { isActive = false }))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task<PagedList<DepartmentListItemDto>> ReadList(HttpClient client, string query) =>
        (await client.GetFromJsonAsync<Envelope<PagedList<DepartmentListItemDto>>>($"/api/departments{query}"))!.Result!;

    private static async Task<IReadOnlyList<DepartmentTreeNodeDto>> ReadTree(HttpClient client) =>
        (await client.GetFromJsonAsync<Envelope<IReadOnlyList<DepartmentTreeNodeDto>>>("/api/departments/tree"))!.Result!;
}
