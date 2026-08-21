using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Cache;
using DirectoryService.Application.Abstractions.Database;
using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Application.Extensions.Validation;
using DirectoryService.Domain.Entities;
using DirectoryService.Domain.Shared;
using DirectoryService.Domain.ValueObjects.Location;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Application.Features.Locations.Create;

public sealed class CreateLocationHandler
    : ICommandHandler<CreateLocationCommand, Guid>
{
    private readonly ILocationRepository _locationRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly IValidator<CreateLocationCommand> _validator;
    private readonly ILogger<CreateLocationHandler> _logger;
    private readonly ICacheInvalidator _cacheInvalidator;

    public CreateLocationHandler(
        ILocationRepository locationRepository,
        ITransactionManager transactionManager,
        IValidator<CreateLocationCommand> validator,
        ILogger<CreateLocationHandler> logger,
        ICacheInvalidator cacheInvalidator)
    {
        _locationRepository = locationRepository;
        _transactionManager = transactionManager;
        _validator = validator;
        _logger = logger;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<Result<Guid, Error>> Handle(
        CreateLocationCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting location creation. Name {LocationName}. Timezone {Timezone}",
            command.Name,
            command.Timezone);

        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning(
                "Location creation validation failed. Errors: {@ValidationErrors}",
                validationResult.Errors.Select(error => new
                {
                    error.PropertyName,
                    error.ErrorMessage,
                }));

            return validationResult.ToError();
        }

        var nameResult = Name.Create(command.Name);
        var addressResult = Address.Create(
            command.Address.Country,
            command.Address.City,
            command.Address.Street,
            command.Address.Building);
        var timeZoneResult = LocationTimeZone.Create(command.Timezone);

        if (nameResult.IsFailure)
            return nameResult.Error;

        if (addressResult.IsFailure)
            return addressResult.Error;

        if (timeZoneResult.IsFailure)
            return timeZoneResult.Error;

        var location = new Location(
            nameResult.Value,
            addressResult.Value,
            timeZoneResult.Value);

        var addResult = await _locationRepository.AddAsync(location, cancellationToken);
        if (addResult.IsFailure)
        {
            _logger.LogWarning(
                "Location creation failed. ErrorType {ErrorType}. Errors: {@ErrorMessages}",
                addResult.Error.Type,
                addResult.Error.Messages);

            return addResult.Error;
        }

        var saveResult = await _transactionManager.SaveChangesAsync(
            "location",
            new { LocationId = location.Id.Value },
            "Location with the same name or address already exists.",
            cancellationToken);
        if (saveResult.IsFailure)
        {
            _logger.LogWarning(
                "Location creation failed while saving. ErrorType {ErrorType}. Errors: {@ErrorMessages}",
                saveResult.Error.Type,
                saveResult.Error.Messages);

            return saveResult.Error;
        }

        _logger.LogInformation(
            "Location created successfully with id {LocationId}",
            location.Id.Value);

        await _cacheInvalidator.InvalidateAsync([CacheConstants.LOCATIONS_CACHE_TAG]);

        return location.Id.Value;
    }
}
