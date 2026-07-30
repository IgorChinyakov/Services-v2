using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Application.Extensions.Validation;
using DirectoryService.Contracts.Common;
using DirectoryService.Contracts.Locations;
using DirectoryService.Domain.Shared;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Application.Features.Locations.Get;

public class GetLocationsHandler : IQueryHandler<GetLocationsQuery, PagedList<LocationDto>>
{
    private readonly ILogger<GetLocationsHandler> _logger;
    private readonly IValidator<GetLocationsQuery> _validator;
    private readonly ILocationRepository _locationRepository;

    public GetLocationsHandler(
        ILogger<GetLocationsHandler> logger,
        IValidator<GetLocationsQuery> validator,
        ILocationRepository locationRepository)
    {
        _logger = logger;
        _validator = validator;
        _locationRepository = locationRepository;
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
                validationResult.Errors.Select(error => new
                {
                    error.PropertyName,
                    error.ErrorMessage,
                }));

            return validationResult.ToError();
        }

        return await _locationRepository.GetFilteredWithPaginationAsync(query, cancellationToken);
    }
}
