using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Application.Exceptions;
using DirectoryService.Contracts.Departments;
using DirectoryService.Domain.Shared;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Application.Features.Departments.GetTree;

public sealed class GetDepartmentTreeHandler
    : IQueryHandler<GetDepartmentTreeQuery, IReadOnlyList<DepartmentTreeNodeDto>>
{
    private readonly IDepartmentQueryRepository _departmentQueryRepository;
    private readonly HybridCache _hybridCache;
    private readonly ILogger<GetDepartmentTreeHandler> _logger;

    public GetDepartmentTreeHandler(
        IDepartmentQueryRepository departmentQueryRepository,
        HybridCache hybridCache,
        ILogger<GetDepartmentTreeHandler> logger)
    {
        _departmentQueryRepository = departmentQueryRepository;
        _hybridCache = hybridCache;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<DepartmentTreeNodeDto>, Error>> HandleAsync(
        GetDepartmentTreeQuery query,
        CancellationToken cancellationToken)
    {
        const string cacheKey = "department_tree:roots";
        var loadedFromDatabase = false;

        try
        {
            var roots = await _hybridCache.GetOrCreateAsync(
                cacheKey,
                factory: async factoryCancellationToken =>
                {
                    loadedFromDatabase = true;
                    var result = await _departmentQueryRepository.GetTreeAsync(factoryCancellationToken);
                    if (result.IsFailure)
                        throw new QueryCacheException(result.Error);

                    return result.Value;
                },
                tags: [CacheConstants.DEPARTMENTS_CACHE_TAG],
                cancellationToken: cancellationToken);

            if (loadedFromDatabase)
                _logger.LogInformation("Department tree roots loaded from database and cached.");
            else
                _logger.LogInformation("Department tree roots loaded from cache.");

            return Result.Success<IReadOnlyList<DepartmentTreeNodeDto>, Error>(roots);
        }
        catch (QueryCacheException exception)
        {
            return exception.Error;
        }
    }
}
