using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Application.Exceptions;
using DirectoryService.Contracts.Common;
using DirectoryService.Contracts.Departments;
using DirectoryService.Domain.Shared;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Application.Features.Departments.GetTopByPositions;

public sealed class GetTopDepartmentsByPositionsHandler
    : IQueryHandler<GetTopDepartmentsByPositionsQuery, IReadOnlyList<TopDepartmentByPositionsDto>>
{
    private readonly IDepartmentQueryRepository _departmentQueryRepository;
    private readonly HybridCache _hybridCache;
    private readonly ILogger<GetTopDepartmentsByPositionsHandler> _logger;

    public GetTopDepartmentsByPositionsHandler(
        IDepartmentQueryRepository departmentQueryRepository,
        HybridCache hybridCache,
        ILogger<GetTopDepartmentsByPositionsHandler> logger)
    {
        _departmentQueryRepository = departmentQueryRepository;
        _hybridCache = hybridCache;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<TopDepartmentByPositionsDto>, Error>> HandleAsync(
        GetTopDepartmentsByPositionsQuery query,
        CancellationToken cancellationToken)
    {
        string cacheKey =
            $"top_departments_by_position";

        IReadOnlyList<TopDepartmentByPositionsDto> topDepartmentByPositionsDtos;
        var loadedFromDatabase = false;

        try
        {
            topDepartmentByPositionsDtos = await _hybridCache.GetOrCreateAsync(
                cacheKey,
                factory: async factoryCancellationToken =>
                {
                    loadedFromDatabase = true;
                    _logger.LogInformation(
                        "Top departments cache miss. Loading data from database. CacheKey: {CacheKey}",
                        cacheKey);

                    var topDepartmentsResult =
                        await _departmentQueryRepository.GetTopByPositionsAsync(factoryCancellationToken);
                    if (topDepartmentsResult.IsFailure)
                        throw new QueryCacheException(topDepartmentsResult.Error);

                    return topDepartmentsResult.Value;
                },
                tags: [CacheConstants.DEPARTMENTS_CACHE_TAG],
                cancellationToken: cancellationToken);
        }
        catch (QueryCacheException e)
        {
            return e.Error;
        }

        if (loadedFromDatabase)
        {
            _logger.LogInformation(
                "Top departments loaded from database and cached. CacheKey: {CacheKey}",
                cacheKey);
        }
        else
        {
            _logger.LogInformation(
                "Top departments cache hit. CacheKey: {CacheKey}",
                cacheKey);
        }

        return Result.Success<IReadOnlyList<TopDepartmentByPositionsDto>, Error>(topDepartmentByPositionsDtos);
    }
}
