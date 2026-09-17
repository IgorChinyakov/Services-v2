using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Application.Exceptions;
using DirectoryService.Application.Extensions.Validation;
using DirectoryService.Contracts.Departments;
using DirectoryService.Domain.Shared;
using FluentValidation;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Application.Features.Departments.SearchTree;

public sealed class SearchDepartmentTreeHandler
    : IQueryHandler<SearchDepartmentTreeQuery, IReadOnlyList<DepartmentTreeSearchResultDto>>
{
    private readonly IDepartmentQueryRepository _departmentQueryRepository;
    private readonly IValidator<SearchDepartmentTreeQuery> _validator;
    private readonly HybridCache _hybridCache;
    private readonly ILogger<SearchDepartmentTreeHandler> _logger;

    public SearchDepartmentTreeHandler(
        IDepartmentQueryRepository departmentQueryRepository,
        IValidator<SearchDepartmentTreeQuery> validator,
        HybridCache hybridCache,
        ILogger<SearchDepartmentTreeHandler> logger)
    {
        _departmentQueryRepository = departmentQueryRepository;
        _validator = validator;
        _hybridCache = hybridCache;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<DepartmentTreeSearchResultDto>, Error>> HandleAsync(
        SearchDepartmentTreeQuery query,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToError();

        var normalizedSearch = query.Search.Trim().ToLowerInvariant();
        var cacheKey = $"department_tree:search:{normalizedSearch}";

        try
        {
            var results = await _hybridCache.GetOrCreateAsync(
                cacheKey,
                factory: async factoryCancellationToken =>
                {
                    var result = await _departmentQueryRepository.SearchTreeAsync(
                        normalizedSearch,
                        factoryCancellationToken);
                    if (result.IsFailure)
                        throw new QueryCacheException(result.Error);

                    return result.Value;
                },
                tags: [CacheConstants.DEPARTMENTS_CACHE_TAG],
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Department tree search completed. Search {Search}. MatchCount {MatchCount}",
                normalizedSearch,
                results.Count);

            return Result.Success<IReadOnlyList<DepartmentTreeSearchResultDto>, Error>(results);
        }
        catch (QueryCacheException exception)
        {
            return exception.Error;
        }
    }
}
