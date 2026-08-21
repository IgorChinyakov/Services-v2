using DirectoryService.Application.Abstractions.Cache;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Infrastructure.Cache;

public sealed class CacheInvalidator : ICacheInvalidator
{
    private readonly ILogger<CacheInvalidator> _logger;
    private readonly HybridCache _hybridCache;

    public CacheInvalidator(ILogger<CacheInvalidator> logger, HybridCache hybridCache)
    {
        _logger = logger;
        _hybridCache = hybridCache;
    }

    public async Task InvalidateAsync(IReadOnlyCollection<string> tags)
    {
        try
        {
            using var token = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await _hybridCache.RemoveByTagAsync(
                tags,
                token.Token);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Cache invalidation failed. Tags: {@CacheTags}", tags);
        }
    }
}
