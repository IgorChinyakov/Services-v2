using Microsoft.Extensions.Caching.Hybrid;

namespace DirectoryService.IntegrationTests;

internal sealed class IsolatedHybridCache(HybridCache inner, TestCacheScope scope) : HybridCache
{
    public override ValueTask<T> GetOrCreateAsync<TState, T>(
        string key,
        TState state,
        Func<TState, CancellationToken, ValueTask<T>> factory,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        var prefix = scope.Prefix;
        return inner.GetOrCreateAsync(
            Key(prefix, key), state, factory, options, Tags(prefix, tags), cancellationToken);
    }

    public override ValueTask SetAsync<T>(
        string key,
        T value,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        var prefix = scope.Prefix;
        return inner.SetAsync(Key(prefix, key), value, options, Tags(prefix, tags), cancellationToken);
    }

    public override ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        inner.RemoveAsync(Key(scope.Prefix, key), cancellationToken);

    public override ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default) =>
        inner.RemoveByTagAsync(Tag(scope.Prefix, tag), cancellationToken);

    private static string Key(string prefix, string key) => string.IsNullOrEmpty(key) ? key : prefix + key;

    private static string Tag(string prefix, string tag) =>
        string.IsNullOrWhiteSpace(tag) || tag == "*" ? tag : prefix + tag;

    // Capture the generation before the asynchronous cache operation starts.
    private static string[]? Tags(string prefix, IEnumerable<string>? tags) =>
        tags?.Select(tag => Tag(prefix, tag)).ToArray();
}
