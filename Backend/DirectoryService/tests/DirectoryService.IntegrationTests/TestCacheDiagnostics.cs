using System.Collections.Concurrent;
using System.Diagnostics.Tracing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace DirectoryService.IntegrationTests;

internal sealed class TestCacheDiagnostics : IDisposable
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private readonly object _lock = new();
    private readonly string? _path = Environment.GetEnvironmentVariable("DIRECTORY_CACHE_DIAGNOSTICS");
    private readonly CacheEventListener? _listener;
    private readonly AsyncLocal<string?> _request = new();
    private string? _test;
    private long _generation;

    public TestCacheDiagnostics()
    {
        if (!string.IsNullOrWhiteSpace(_path))
            _listener = new CacheEventListener(this);
    }

    public void SetTest(string test) => _test = test;

    public void BeginReset() => Interlocked.Increment(ref _generation);

    public void SetRequest(string? request) => _request.Value = request;

    public void Snapshot(string phase, HybridCache cache, HybridCache? resetCache)
    {
        if (string.IsNullOrWhiteSpace(_path))
            return;

        try
        {
            var local = cache.GetType().GetField("_localCache", Members)?.GetValue(cache) as MemoryCache;
            var tags = cache.GetType().GetField("_tagInvalidationTimes", Members)?.GetValue(cache)
                as ConcurrentDictionary<string, Task<long>>;
            var clock = cache.GetType().GetField("_clock", Members)?.GetValue(cache) as TimeProvider;
            var entries = local?.Keys.OfType<string>().OrderBy(key => key).Take(32).Select(key =>
            {
                local.TryGetValue(key, out object? entry);
                var entryTags = Property(entry, "Tags");
                var tagCount = Property(entryTags, "Count");
                var tag = tagCount is int count && count == 1
                    ? entryTags?.GetType().GetMethod("GetSinglePrechecked", Members)?.Invoke(entryTags, null)
                    : null;
                return new
                {
                    key,
                    created = Property(entry, "CreationTimestamp"),
                    tagCount,
                    tag,
                };
            }).ToArray();

            Write(new
            {
                phase,
                test = _test,
                generation = _generation,
                request = _request.Value,
                utcTicks = DateTime.UtcNow.Ticks,
                cacheTicks = clock?.GetUtcNow().UtcTicks,
                cacheId = RuntimeHelpers.GetHashCode(cache),
                resetCacheId = resetCache is null ? (int?)null : RuntimeHelpers.GetHashCode(resetCache),
                cacheAssembly = cache.GetType().Assembly.FullName,
                localCount = local?.Count,
                tags = tags?.ToDictionary(pair => pair.Key, pair => pair.Value.IsCompletedSuccessfully
                    ? pair.Value.Result.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    : pair.Value.Status.ToString()),
                entries,
            });
        }
        catch (Exception exception)
        {
            Write(new { phase = "diagnostics-error", message = exception.ToString() });
        }
    }

    public void Dispose() => _listener?.Dispose();

    private static object? Property(object? instance, string name)
    {
        for (var type = instance?.GetType(); type is not null; type = type.BaseType)
        {
            var property = type.GetProperty(name, Members | BindingFlags.DeclaredOnly);
            if (property is not null)
                return property.GetValue(instance);
        }

        return null;
    }

    private void Write(object data)
    {
        if (string.IsNullOrWhiteSpace(_path))
            return;

        lock (_lock)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.AppendAllText(_path, JsonSerializer.Serialize(data) + Environment.NewLine);
        }
    }

    private sealed class CacheEventListener : EventListener
    {
        private readonly TestCacheDiagnostics? _diagnostics;

        public CacheEventListener(TestCacheDiagnostics diagnostics)
        {
            _diagnostics = diagnostics;
            foreach (var source in EventSource.GetSources())
                OnEventSourceCreated(source);
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (_diagnostics is not null && eventSource.Name.Contains("Hybrid", StringComparison.OrdinalIgnoreCase))
                EnableEvents(eventSource, EventLevel.Verbose, EventKeywords.All);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            _diagnostics?.Write(new
            {
                phase = "cache-event",
                test = _diagnostics._test,
                generation = _diagnostics._generation,
                request = _diagnostics._request.Value,
                utcTicks = DateTime.UtcNow.Ticks,
                name = eventData.EventName,
            });
        }
    }
}

internal sealed class TestCacheDiagnosticsStartupFilter(DirectoryServiceWebFactory factory) : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (context, continuation) =>
        {
            var cache = context.RequestServices.GetRequiredService<HybridCache>();
            factory.SetDiagnosticRequest($"{context.Request.Method} {context.Request.Path}{context.Request.QueryString}");
            factory.SnapshotCache("request-before", cache);
            try
            {
                await continuation();
            }
            finally
            {
                factory.SnapshotCache("request-after", cache);
                factory.SetDiagnosticRequest(null);
            }
        });
        next(app);
    };
}
