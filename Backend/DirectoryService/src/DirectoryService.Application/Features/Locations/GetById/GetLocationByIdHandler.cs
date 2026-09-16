using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Database;
using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Extensions.Validation;
using DirectoryService.Contracts.Common;
using DirectoryService.Contracts.Locations;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.Shared;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Application.Features.Locations.GetById;

public sealed class GetLocationByIdHandler
    : IQueryHandler<GetLocationByIdQuery, LocationDetailsDto>
{
    private readonly IReadDbContext _readDbContext;
    private readonly IValidator<GetLocationByIdQuery> _validator;
    private readonly ILogger<GetLocationByIdHandler> _logger;

    public GetLocationByIdHandler(
        IReadDbContext readDbContext,
        IValidator<GetLocationByIdQuery> validator,
        ILogger<GetLocationByIdHandler> logger)
    {
        _readDbContext = readDbContext;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<LocationDetailsDto, Error>> HandleAsync(
        GetLocationByIdQuery query,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToError();

        var locationId = LocationId.Create(query.Id);
        var location = await _readDbContext.Locations
            .Where(item => item.Id == locationId)
            .Select(item => new LocationDetailsDto(
                item.Id.Value,
                item.Name.Value,
                new AddressDto
                {
                    Country = item.Address.Country,
                    City = item.Address.City,
                    Street = item.Address.Street,
                    Building = item.Address.Building,
                },
                item.TimeZone.Value,
                item.IsActive,
                item.CreatedAt,
                item.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (location is null)
        {
            _logger.LogWarning("Location {LocationId} was not found", query.Id);

            return GeneralErrors.NotFound(
                $"Location with id '{query.Id}' was not found.",
                nameof(query.Id));
        }

        _logger.LogInformation("Location {LocationId} was loaded", query.Id);

        return location;
    }
}
