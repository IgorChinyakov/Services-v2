# Код из урока по HybridCache и Redis

Ниже собрана цельная версия кода, который автор писал в уроке. Это реконструкция
по фрагментам с экрана, а не копия исходного репозитория. Названия основных типов
и логика сохранены.

## Пакеты

```xml
<PackageReference Include="Microsoft.Extensions.Caching.Hybrid" />
<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" />
```

`HybridCache` организует двухуровневый кэш:

- L1: память текущего экземпляра приложения;
- L2: распределённый Redis через `IDistributedCache`.

## Redis в Docker Compose

Минимальная конфигурация, эквивалентная показанной в уроке:

```yaml
services:
  redis:
    image: redis:7-alpine
    container_name: file_service_redis
    restart: unless-stopped
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data

volumes:
  redis_data:
```

## Регистрация кэширования

```csharp
public static IServiceCollection AddCore(
    this IServiceCollection services,
    IConfiguration configuration)
{
    services.AddValidatorsFromAssembly(
        typeof(DependencyInjectionCoreExtensions).Assembly);

    services.AddScoped<StartMultipartUploadHandler>();
    services.AddScoped<CompleteMultipartUploadHandler>();

    services.AddStackExchangeRedisCache(setup =>
    {
        setup.Configuration = "localhost:6379";
    });

    services.AddHybridCache(options =>
    {
        options.DefaultEntryOptions = new HybridCacheEntryOptions
        {
            LocalCacheExpiration = TimeSpan.FromMinutes(5),
            Expiration = TimeSpan.FromMinutes(30)
        };
    });

    return services;
}
```

Здесь `LocalCacheExpiration` задаёт TTL в памяти приложения, а `Expiration` -
TTL записи в Redis.

## Настройки файлового хранилища

```csharp
public record FileStorageOptions
{
    public string Endpoint { get; init; } = string.Empty;
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public bool WithSsl { get; init; }
    public int DownloadUrlExpirationDays { get; init; } = 6;
    public IReadOnlyList<string> RequiredBuckets { get; init; } = [];
    public double UploadUrlExpirationHours { get; init; } = 1;
    public int MaxConcurrentRequests { get; init; } = 20;
    public long RecommendedChunkSizeBytes { get; init; } = 100 * 1024 * 1024;
    public int MaxChunks { get; init; } = 100;
}
```

## Handler

```csharp
public sealed class GetMediaAssetsUploadHandler
{
    private readonly IReadDbContext _readDbContext;
    private readonly IFileStorageProvider _fileStorageProvider;
    private readonly HybridCache _cache;
    private readonly FileStorageOptions _fileStorageOptions;

    public GetMediaAssetsUploadHandler(
        IReadDbContext readDbContext,
        IFileStorageProvider fileStorageProvider,
        HybridCache cache,
        IOptions<FileStorageOptions> fileStorageOptions)
    {
        _readDbContext = readDbContext;
        _fileStorageProvider = fileStorageProvider;
        _cache = cache;
        _fileStorageOptions = fileStorageOptions.Value;
    }

    public async Task<Result<GetMediaAssetsResponse, Error>> Handle(
        GetMediaAssetsRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.MediaAssetIds.Any())
            return new GetMediaAssetsResponse([]);

        List<MediaAsset> mediaAssets = await _readDbContext.MediaAssetsQuery
            .Where(mediaAsset =>
                request.MediaAssetIds.Contains(mediaAsset.Id) &&
                mediaAsset.Status != MediaStatus.DELETED)
            .ToListAsync(cancellationToken);

        List<MediaAsset> readyMediaAssets = mediaAssets
            .Where(mediaAsset => mediaAsset.Status == MediaStatus.READY)
            .ToList();

        List<StorageKey> keys = readyMediaAssets
            .Select(mediaAsset => mediaAsset.Key)
            .ToList();

        Dictionary<StorageKey, string> urls =
            await GetPresignedUrlsFromCache(keys, cancellationToken);

        var results = new List<GetMediaAssetsDto>();

        foreach (MediaAsset mediaAsset in mediaAssets)
        {
            string? downloadUrl = null;

            if (urls.TryGetValue(mediaAsset.Key, out string? url))
                downloadUrl = url;

            var mediaAssetDto = new GetMediaAssetsDto(
                mediaAsset.Id,
                mediaAsset.Status.ToString().ToLowerInvariant(),
                mediaAsset.AssetType.ToString().ToLowerInvariant(),
                downloadUrl);

            results.Add(mediaAssetDto);
        }

        return new GetMediaAssetsResponse(results);
    }

    private async Task<Dictionary<StorageKey, string>> GetPresignedUrlsFromCache(
        IEnumerable<StorageKey> storageKeys,
        CancellationToken cancellationToken)
    {
        List<StorageKey> keys = storageKeys.ToList();

        if (!keys.Any())
            return [];

        IEnumerable<Task<(StorageKey Key, string? Url)>> cachedUrlTasks =
            keys.Select(async key =>
            {
                string? url = await _cache.GetOrCreateAsync<string?>(
                    key: key.Value,
                    factory: _ => ValueTask.FromResult<string?>(null),
                    options: new HybridCacheEntryOptions
                    {
                        Expiration = TimeSpan
                            .FromDays(_fileStorageOptions.DownloadUrlExpirationDays)
                            .Subtract(TimeSpan.FromHours(1)),
                        LocalCacheExpiration = TimeSpan.FromHours(1)
                    },
                    cancellationToken: cancellationToken);

                return (key, url);
            });

        (StorageKey Key, string? Url)[] cachedUrls =
            await Task.WhenAll(cachedUrlTasks);

        var result = new Dictionary<StorageKey, string>();
        var keysToGenerate = new List<StorageKey>();

        foreach ((StorageKey key, string? url) in cachedUrls)
        {
            if (!string.IsNullOrWhiteSpace(url))
                result[key] = url;
            else
                keysToGenerate.Add(key);
        }

        if (!keysToGenerate.Any())
            return result;

        Result<IReadOnlyList<MediaUrl>, Error> mediaUrls =
            await _fileStorageProvider.GenerateDownloadUrlsAsync(
                keysToGenerate,
                cancellationToken);

        if (mediaUrls.IsFailure)
            return result;

        IEnumerable<Task> setTasks = mediaUrls.Value.Select(async mediaUrl =>
        {
            result[mediaUrl.StorageKey] = mediaUrl.PresignedUrl;

            await _cache.SetAsync(
                key: mediaUrl.StorageKey.Value,
                value: mediaUrl.PresignedUrl,
                options: new HybridCacheEntryOptions
                {
                    Expiration = TimeSpan
                        .FromDays(_fileStorageOptions.DownloadUrlExpirationDays)
                        .Subtract(TimeSpan.FromHours(1))
                },
                cancellationToken: cancellationToken);
        });

        await Task.WhenAll(setTasks);

        return result;
    }
}
```

## Как выполняется алгоритм

1. Handler получает из PostgreSQL медиафайлы и оставляет только готовые файлы.
2. Для каждого `StorageKey` параллельно проверяется `HybridCache`.
3. Найденные URL сразу добавляются в итоговый словарь.
4. Ключи, которых нет в кэше, собираются в `keysToGenerate`.
5. Для всех отсутствующих ключей выполняется один пакетный запрос в S3.
6. Новые URL параллельно записываются в Redis и возвращаются клиенту.
7. Redis хранит URL на час меньше срока действия ссылки, поэтому просроченная
   presigned-ссылка не должна попасть клиенту.

Фабрика `ValueTask.FromResult<string?>(null)` намеренно ничего не загружает из
S3. В этом месте `GetOrCreateAsync` используется фактически как попытка чтения:
при промахе возвращается `null`, а пакетная генерация ссылок выполняется позже.

## Что важно при переносе в Directory Service

Для подразделений схема будет проще:

```text
ключ с параметрами запроса -> HybridCache -> query repository -> PostgreSQL
```

В `factory` уже нужно вызывать query repository, потому что результат запроса
получается одним объектом. Ключ обязан включать все параметры, влияющие на ответ:
маршрут, `parentId`, страницу, размер страницы, `prefetch`, фильтры и сортировку.

Пример:

```csharp
string cacheKey =
    $"departments:children:{query.ParentId}:page={query.Page}:size={query.Size}";

return await cache.GetOrCreateAsync(
    cacheKey,
    async cancellationToken =>
        await repository.GetChildrenAsync(query, cancellationToken),
    new HybridCacheEntryOptions
    {
        LocalCacheExpiration = TimeSpan.FromMinutes(1),
        Expiration = TimeSpan.FromMinutes(5)
    },
    cancellationToken: cancellationToken);
```

После создания, перемещения, soft delete и изменения подразделения связанные
ключи нужно инвалидировать. Для этого удобнее использовать cache tags или ключ
версии, чем пытаться перечислить все варианты пагинации вручную.
