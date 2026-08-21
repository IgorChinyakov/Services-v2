using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Application.Exceptions;
using DirectoryService.Application.Extensions.Validation;
using DirectoryService.Contracts.Common;
using DirectoryService.Contracts.Departments;
using DirectoryService.Contracts.Locations;
using DirectoryService.Domain.Shared;
using FluentValidation;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Application.Features.Departments.GetChildren;

public sealed class GetDepartmentChildrenHandler
    : IQueryHandler<GetDepartmentChildrenQuery, PagedList<DepartmentNodeDto>>
{
    private readonly IDepartmentQueryRepository _departmentQueryRepository;
    private readonly IValidator<GetDepartmentChildrenQuery> _validator;
    private readonly ILogger<GetDepartmentChildrenHandler> _logger;
    private readonly HybridCache _hybridCache;

    public GetDepartmentChildrenHandler(
        IDepartmentQueryRepository departmentQueryRepository,
        IValidator<GetDepartmentChildrenQuery> validator,
        ILogger<GetDepartmentChildrenHandler> logger, HybridCache hybridCache)
    {
        _departmentQueryRepository = departmentQueryRepository;
        _validator = validator;
        _logger = logger;
        _hybridCache = hybridCache;
    }

    public async Task<Result<PagedList<DepartmentNodeDto>, Error>> HandleAsync(
        GetDepartmentChildrenQuery query,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning(
                "Department children query validation failed. Errors: {@ValidationErrors}",
                validationResult.Errors);

            return validationResult.ToError();
        }

        string cacheKey =
            $"children_departments:page={query.Page}:size={query.Size}:parentId={query.ParentId}";

        PagedList<DepartmentNodeDto> childrenDepartmentsPagedList;
        var loadedFromDatabase = false;

        try
        {
            childrenDepartmentsPagedList = await _hybridCache.GetOrCreateAsync(
                cacheKey,
                factory: async factoryCancellationToken =>
                {
                    loadedFromDatabase = true;
                    _logger.LogInformation(
                        "Department children cache miss. Loading data from database. CacheKey: {CacheKey}",
                        cacheKey);

                    var childrenResult =
                        await _departmentQueryRepository.GetChildrenAsync(query, factoryCancellationToken);
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
                "Department children loaded from database and cached. CacheKey: {CacheKey}",
                cacheKey);
        }
        else
        {
            _logger.LogInformation(
                "Department children cache hit. CacheKey: {CacheKey}",
                cacheKey);
        }

        return childrenDepartmentsPagedList;
    }
}
