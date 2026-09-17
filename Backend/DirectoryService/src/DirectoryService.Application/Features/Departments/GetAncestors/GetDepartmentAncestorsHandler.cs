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

namespace DirectoryService.Application.Features.Departments.GetAncestors;

public sealed class GetDepartmentAncestorsHandler
    : IQueryHandler<GetDepartmentAncestorsQuery, IReadOnlyList<DepartmentTreeNodeDto>>
{
    private readonly IDepartmentQueryRepository _departmentQueryRepository;
    private readonly IValidator<GetDepartmentAncestorsQuery> _validator;
    private readonly HybridCache _hybridCache;
    private readonly ILogger<GetDepartmentAncestorsHandler> _logger;

    public GetDepartmentAncestorsHandler(
        IDepartmentQueryRepository departmentQueryRepository,
        IValidator<GetDepartmentAncestorsQuery> validator,
        HybridCache hybridCache,
        ILogger<GetDepartmentAncestorsHandler> logger)
    {
        _departmentQueryRepository = departmentQueryRepository;
        _validator = validator;
        _hybridCache = hybridCache;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<DepartmentTreeNodeDto>, Error>> HandleAsync(
        GetDepartmentAncestorsQuery query,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToError();

        var cacheKey = $"department_tree:ancestors:{query.DepartmentId}";

        try
        {
            var ancestors = await _hybridCache.GetOrCreateAsync(
                cacheKey,
                factory: async factoryCancellationToken =>
                {
                    var result = await _departmentQueryRepository.GetAncestorsAsync(
                        query.DepartmentId,
                        factoryCancellationToken);
                    if (result.IsFailure)
                        throw new QueryCacheException(result.Error);

                    return result.Value;
                },
                tags: [CacheConstants.DEPARTMENTS_CACHE_TAG],
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Department ancestors loaded. DepartmentId {DepartmentId}. Count {AncestorCount}",
                query.DepartmentId,
                ancestors.Count);

            return Result.Success<IReadOnlyList<DepartmentTreeNodeDto>, Error>(ancestors);
        }
        catch (QueryCacheException exception)
        {
            return exception.Error;
        }
    }
}
