using System.Security.Cryptography;
using System.Text;
using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Application.Exceptions;
using DirectoryService.Application.Extensions.Validation;
using DirectoryService.Contracts.Common;
using DirectoryService.Contracts.Locations;
using DirectoryService.Domain.Shared;
using FluentValidation;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Application.Features.Locations.Get;

public class GetLocationsHandler : IQueryHandler<GetLocationsQuery, PagedList<LocationDto>>
{
    private readonly ILogger<GetLocationsHandler> _logger;
    private readonly IValidator<GetLocationsQuery> _validator;
    private readonly ILocationQueryRepository _locationQueryRepository;
    private readonly HybridCache _hybridCache;

    public GetLocationsHandler(
        ILogger<GetLocationsHandler> logger,
        IValidator<GetLocationsQuery> validator,
        ILocationQueryRepository locationQueryRepository,
        HybridCache hybridCache)
    {
        _logger = logger;
        _validator = validator;
        _locationQueryRepository = locationQueryRepository;
        _hybridCache = hybridCache;
    }

    public async Task<Result<PagedList<LocationDto>, Error>> HandleAsync(
        GetLocationsQuery query,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning(
                "Location receiving validation failed. Errors: {@ValidationErrors}",
                validationResult.Errors.Select(error => new { error.PropertyName, error.ErrorMessage, }));

            return validationResult.ToError();
        }

        var departmentsHash = GetDepartmentIdsHash(query.DepartmentIds);
        var search = query.Search?.Trim().ToLowerInvariant() ?? string.Empty;
        var isActive = query.IsActive is null ? string.Empty : query.IsActive.ToString();

        string cacheKey =
            $"locations:page={query.Page}:size={query.PageSize}" +
            $":search={search}:active={isActive}" +
            $":sort={query.SortBy}:direction={query.SortDirection}" +
            $":department={departmentsHash}";

        PagedList<LocationDto>? locationPagedList;
        var loadedFromDatabase = false;

        try
        {
            locationPagedList = await _hybridCache.GetOrCreateAsync(
                cacheKey,
                factory: async factoryCancellationToken =>
                {
                    loadedFromDatabase = true;
                    _logger.LogInformation(
                        "Locations cache miss. Loading data from database. CacheKey: {CacheKey}",
                        cacheKey);

                    var locationsResult =
                        await _locationQueryRepository.GetFilteredWithPaginationAsync(query, factoryCancellationToken);
                    if (locationsResult.IsFailure)
                        throw new QueryCacheException(locationsResult.Error);

                    return locationsResult.Value;
                },
                tags: [CacheConstants.LOCATIONS_CACHE_TAG],
                cancellationToken: cancellationToken);
        }
        catch (QueryCacheException e)
        {
            return e.Error;
        }

        if (loadedFromDatabase)
        {
            _logger.LogInformation(
                "Locations loaded from database and cached. CacheKey: {CacheKey}",
                cacheKey);
        }
        else
        {
            _logger.LogInformation(
                "Locations cache hit. CacheKey: {CacheKey}",
                cacheKey);
        }

        return locationPagedList;
    }

    private static string GetDepartmentIdsHash(Guid[]? departmentIds)
    {
        if (departmentIds is null)
            return "none";

        string normalizedIds = string.Join(
            ',',
            departmentIds
                .Order()
                .Select(id => id.ToString("N")));

        byte[] sourceBytes = Encoding.UTF8.GetBytes(normalizedIds);
        byte[] hashBytes = SHA256.HashData(sourceBytes);

        return Convert.ToHexString(hashBytes);
    }
}
