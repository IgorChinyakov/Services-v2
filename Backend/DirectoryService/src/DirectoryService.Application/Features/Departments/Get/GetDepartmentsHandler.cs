using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Database;
using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Exceptions;
using DirectoryService.Application.Extensions.Validation;
using DirectoryService.Contracts.Common;
using DirectoryService.Contracts.Departments;
using DirectoryService.Contracts.Departments.Sort;
using DirectoryService.Domain.Entities;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.Errors;
using DirectoryService.Domain.Shared;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Application.Features.Departments.Get;

public sealed class GetDepartmentsHandler
    : IQueryHandler<GetDepartmentsQuery, PagedList<DepartmentListItemDto>>
{
    private readonly IReadDbContext _readDbContext;
    private readonly IValidator<GetDepartmentsQuery> _validator;
    private readonly HybridCache _hybridCache;
    private readonly ILogger<GetDepartmentsHandler> _logger;

    public GetDepartmentsHandler(
        IReadDbContext readDbContext,
        IValidator<GetDepartmentsQuery> validator,
        HybridCache hybridCache,
        ILogger<GetDepartmentsHandler> logger)
    {
        _readDbContext = readDbContext;
        _validator = validator;
        _hybridCache = hybridCache;
        _logger = logger;
    }

    public async Task<Result<PagedList<DepartmentListItemDto>, Error>> HandleAsync(
        GetDepartmentsQuery query,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning(
                "Department list validation failed. Errors: {@ValidationErrors}",
                validationResult.Errors.Select(error => new { error.PropertyName, error.ErrorMessage, }));

            return validationResult.ToError();
        }

        var cacheKey = BuildCacheKey(query);
        var loadedFromDatabase = false;

        PagedList<DepartmentListItemDto> departments;
        try
        {
            departments = await _hybridCache.GetOrCreateAsync(
                cacheKey,
                factory: async factoryCancellationToken =>
                {
                    loadedFromDatabase = true;

                    _logger.LogInformation(
                        "Department list cache miss. Loading data from database. CacheKey {CacheKey}",
                        cacheKey);

                    return await LoadAsync(query, factoryCancellationToken);
                },
                tags: [CacheConstants.DEPARTMENTS_CACHE_TAG],
                cancellationToken: cancellationToken);
        }
        catch (QueryCacheException exception)
        {
            return exception.Error;
        }

        _logger.LogInformation(
            loadedFromDatabase
                ? "Department list loaded from database and cached. ItemCount {ItemCount}. CacheKey {CacheKey}"
                : "Department list loaded from cache. ItemCount {ItemCount}. CacheKey {CacheKey}",
            departments.Items.Count,
            cacheKey);

        return departments;
    }

    private static IQueryable<Department> ApplySearch(
        IQueryable<Department> departments,
        string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return departments;

        var normalizedSearch = search.Trim().ToLowerInvariant();

        return departments.Where(department =>
            department.Name.Value.ToLower().Contains(normalizedSearch));
    }

    private static IOrderedQueryable<Department> ApplySorting(
        IQueryable<Department> departments,
        DepartmentSortBy sortBy,
        SortDirection sortDirection)
    {
        return (sortBy, sortDirection) switch
        {
            (DepartmentSortBy.Name, SortDirection.Asc) => departments
                .OrderBy(department => department.Name.Value)
                .ThenBy(department => department.Id),
            (DepartmentSortBy.Name, SortDirection.Desc) => departments
                .OrderByDescending(department => department.Name.Value)
                .ThenBy(department => department.Id),
            (DepartmentSortBy.CreatedAt, SortDirection.Asc) => departments
                .OrderBy(department => department.CreatedAt)
                .ThenBy(department => department.Id),
            (DepartmentSortBy.CreatedAt, SortDirection.Desc) => departments
                .OrderByDescending(department => department.CreatedAt)
                .ThenBy(department => department.Id),
            _ => throw new ArgumentOutOfRangeException(nameof(sortBy)),
        };
    }

    private static string BuildCacheKey(GetDepartmentsQuery query)
    {
        var search = query.Search?.Trim().ToLowerInvariant() ?? string.Empty;
        var locationIds = NormalizeIds(query.LocationIds);
        var excludeIds = NormalizeIds(query.ExcludeIds);

        return $"departments:list:page={query.Page}:size={query.PageSize}" +
               $":search={search}:sort={query.SortBy}:direction={query.SortDirection}" +
               $":active={query.IsActive ?? true}:parent={query.ParentId}" +
               $":locations={locationIds}:exclude={excludeIds}" +
               $":excludeSubtreeOf={query.ExcludeSubtreeOf}";
    }

    private static string NormalizeIds(Guid[]? ids) =>
        ids is { Length: > 0 }
            ? string.Join(',', ids.Distinct().Order())
            : string.Empty;

    private async Task<PagedList<DepartmentListItemDto>> LoadAsync(
        GetDepartmentsQuery query,
        CancellationToken cancellationToken)
    {
        var departments = _readDbContext.Departments;

        if (query.IsActive == false)
        {
            departments = departments.IgnoreQueryFilters()
                .Where(department => !department.IsActive);
        }

        if (query.ParentId is { } parentId)
        {
            var departmentParentId = DepartmentId.Create(parentId);
            departments = departments.Where(department => department.ParentId == departmentParentId);
        }

        if (query.LocationIds is { Length: > 0 })
        {
            var locationIds = query.LocationIds.Select(LocationId.Create).ToArray();
            departments = departments.Where(department => department.DepartmentLocations.Any(link =>
                locationIds.Contains(link.LocationId) && link.Location.IsActive));
        }

        if (query.ExcludeIds is { Length: > 0 })
        {
            var excludeIds = query.ExcludeIds.Select(DepartmentId.Create).ToArray();
            departments = departments.Where(department => !excludeIds.Contains(department.Id));
        }

        if (query.ExcludeSubtreeOf is { } excludedDepartmentId)
        {
            var subtreePath = await _readDbContext.Departments
                .IgnoreQueryFilters()
                .Where(department => department.Id == DepartmentId.Create(excludedDepartmentId))
                .Select(department => department.Path)
                .SingleOrDefaultAsync(cancellationToken);

            if (subtreePath is null)
            {
                throw new QueryCacheException(DepartmentErrors.NotFound(
                    excludedDepartmentId, nameof(query.ExcludeSubtreeOf)));
            }

            departments = departments.Where(department =>
                !((LTree)department.Path).IsDescendantOf((LTree)subtreePath));
        }

        departments = ApplySearch(departments, query.Search);

        var totalCount = await departments.LongCountAsync(cancellationToken);
        var offset = ((long)query.Page - 1) * query.PageSize;

        if (offset >= totalCount)
        {
            return PagedList<DepartmentListItemDto>.Create(
                [],
                query.Page,
                query.PageSize,
                totalCount);
        }

        var items = await ApplySorting(departments, query.SortBy, query.SortDirection)
            .Skip((int)offset)
            .Take(query.PageSize)
            .Select(department => new DepartmentListItemDto(
                department.Id.Value,
                department.Name.Value,
                department.Slug.Value,
                department.Path,
                department.IsActive,
                department.CreatedAt,
                department.UpdatedAt,
                department.DeletedAt))
            .ToListAsync(cancellationToken);

        return PagedList<DepartmentListItemDto>.Create(
            items,
            query.Page,
            query.PageSize,
            totalCount);
    }
}
