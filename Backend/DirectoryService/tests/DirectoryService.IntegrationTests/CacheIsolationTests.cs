using System.Net.Http.Json;
using DirectoryService.Application;
using DirectoryService.Contracts.Departments;
using DirectoryService.Presentation.ApiResponse;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace DirectoryService.IntegrationTests;

[Collection(DirectoryServiceTestCollection.Name)]
public sealed class CacheIsolationTests : DirectoryServiceTestsBase
{
    private readonly DirectoryServiceWebFactory _factory;

    public CacheIsolationTests(DirectoryServiceWebFactory factory)
        : base(factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ResetDatabase_ShouldIsolateTaggedAndUntaggedEntriesInBothCacheLayers()
    {
        var cache = Services.GetRequiredService<HybridCache>();
        await cache.SetAsync("isolation:untagged", "old");
        await cache.SetAsync("isolation:tagged", "old", tags: [CacheConstants.DEPARTMENTS_CACHE_TAG]);

        await _factory.ResetDatabaseAsync();

        var loads = 0;
        foreach (var key in new[] { "isolation:untagged", "isolation:tagged" })
        {
            var value = await cache.GetOrCreateAsync(key, _ =>
            {
                loads++;
                return ValueTask.FromResult("new");
            });
            value.Should().Be("new");

            var repeated = await cache.GetOrCreateAsync(key, _ => ValueTask.FromResult("unexpected"));
            repeated.Should().Be("new");

            var distributed = await cache.GetOrCreateAsync(
                key,
                _ => ValueTask.FromResult("new"),
                options: new HybridCacheEntryOptions { Flags = HybridCacheEntryFlags.DisableLocalCacheRead });
            distributed.Should().Be("new");
        }

        loads.Should().Be(2);
    }

    [Fact]
    public async Task ResetDatabase_WhenTreeIsCached_ShouldReadOnlyTheNewDatabaseState()
    {
        var oldId = await SeedDepartmentAsync("Old Root", "oldroot");
        using var client = CreateClient();
        var before = await client.GetFromJsonAsync<Envelope<IReadOnlyList<DepartmentTreeNodeDto>>>(
            "/api/departments/tree");
        before!.Result.Should().ContainSingle(item => item.Id == oldId.Value);

        await _factory.ResetDatabaseAsync();
        var newId = await SeedDepartmentAsync("New Root", "newroot");

        var after = await client.GetFromJsonAsync<Envelope<IReadOnlyList<DepartmentTreeNodeDto>>>(
            "/api/departments/tree");
        after!.Result.Should().ContainSingle(item => item.Id == newId.Value);
    }
}
