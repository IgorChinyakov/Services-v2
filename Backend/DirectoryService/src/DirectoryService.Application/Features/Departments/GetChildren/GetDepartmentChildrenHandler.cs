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

namespace DirectoryService.Application.Features.Departments.GetChildren;

public sealed class GetDepartmentChildrenHandler
    : IQueryHandler<GetDepartmentChildrenQuery, IReadOnlyList<DepartmentTreeNodeDto>>
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

    public async Task<Result<IReadOnlyList<DepartmentTreeNodeDto>, Error>> HandleAsync(
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

        var cacheKey = $"department_tree:children:{query.DepartmentId}";

        IReadOnlyList<DepartmentTreeNodeDto> children;
        var loadedFromDatabase = false;

        try
        {
            children = await _hybridCache.GetOrCreateAsync(
                cacheKey,
                factory: async factoryCancellationToken =>
                {
                    loadedFromDatabase = true;
                    _logger.LogInformation(
                        "Department children cache miss. Loading data from database. CacheKey: {CacheKey}",
                        cacheKey);

                    var childrenResult =
                        await _departmentQueryRepository.GetChildrenAsync(
                            query.DepartmentId,
                            factoryCancellationToken);
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

        return Result.Success<IReadOnlyList<DepartmentTreeNodeDto>, Error>(children);
    }
}
