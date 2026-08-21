using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Application.Exceptions;
using DirectoryService.Application.Extensions.Validation;
using DirectoryService.Contracts.Common;
using DirectoryService.Contracts.Departments;
using DirectoryService.Domain.Shared;
using FluentValidation;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Application.Features.Departments.GetRoots;

public sealed class GetRootDepartmentsHandler
    : IQueryHandler<GetRootDepartmentsQuery, PagedList<RootDepartmentDto>>
{
    private readonly IDepartmentQueryRepository _departmentQueryRepository;
    private readonly IValidator<GetRootDepartmentsQuery> _validator;
    private readonly ILogger<GetRootDepartmentsHandler> _logger;
    private readonly HybridCache _hybridCache;

    public GetRootDepartmentsHandler(
        IDepartmentQueryRepository departmentQueryRepository,
        IValidator<GetRootDepartmentsQuery> validator,
        ILogger<GetRootDepartmentsHandler> logger, HybridCache hybridCache)
    {
        _departmentQueryRepository = departmentQueryRepository;
        _validator = validator;
        _logger = logger;
        _hybridCache = hybridCache;
    }

    public async Task<Result<PagedList<RootDepartmentDto>, Error>> HandleAsync(
        GetRootDepartmentsQuery query,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning(
                "Root departments query validation failed. Errors: {@ValidationErrors}",
                validationResult.Errors);

            return validationResult.ToError();
        }

        string cacheKey =
            $"root_departments:page={query.Page}:size={query.Size}:prefetch={query.Prefetch}";

        PagedList<RootDepartmentDto> rootDepartmentsPagedList;
        var loadedFromDatabase = false;

        try
        {
            rootDepartmentsPagedList = await _hybridCache.GetOrCreateAsync(
                cacheKey,
                factory: async factoryCancellationToken =>
                {
                    loadedFromDatabase = true;
                    _logger.LogInformation(
                        "Root departments cache miss. Loading data from database. CacheKey: {CacheKey}",
                        cacheKey);

                    var childrenResult =
                        await _departmentQueryRepository.GetRootsAsync(query, factoryCancellationToken);
                    if (childrenResult.IsFailure)
                        throw new QueryCacheException(childrenResult.Error);

                    return childrenResult.Value;
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
                "Root departments loaded from database and cached. CacheKey: {CacheKey}",
                cacheKey);
        }
        else
        {
            _logger.LogInformation(
                "Root departments cache hit. CacheKey: {CacheKey}",
                cacheKey);
        }

        return rootDepartmentsPagedList;
    }
}
