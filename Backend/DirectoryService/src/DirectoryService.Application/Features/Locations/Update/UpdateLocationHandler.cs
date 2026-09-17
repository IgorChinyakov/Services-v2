using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Cache;
using DirectoryService.Application.Abstractions.Database;
using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Application.Extensions.Validation;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.Errors;
using DirectoryService.Domain.Shared;
using DirectoryService.Domain.ValueObjects.Location;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Application.Features.Locations.Update;

public sealed class UpdateLocationHandler : ICommandHandler<UpdateLocationCommand>
{
    private readonly ILocationRepository _locationRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly IValidator<UpdateLocationCommand> _validator;
    private readonly ILogger<UpdateLocationHandler> _logger;
    private readonly ICacheInvalidator _cacheInvalidator;

    public UpdateLocationHandler(
        ILocationRepository locationRepository,
        ITransactionManager transactionManager,
        IValidator<UpdateLocationCommand> validator,
        ILogger<UpdateLocationHandler> logger,
        ICacheInvalidator cacheInvalidator)
    {
        _locationRepository = locationRepository;
        _transactionManager = transactionManager;
        _validator = validator;
        _logger = logger;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<UnitResult<Error>> Handle(
        UpdateLocationCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToError();

        var nameResult = Name.Create(command.Name);
        if (nameResult.IsFailure)
            return nameResult.Error;

        var addressResult = Address.Create(
            command.Address.City,
            command.Address.Street,
            command.Address.House,
            command.Address.Apartment);
        if (addressResult.IsFailure)
            return addressResult.Error;

        var locationId = LocationId.Create(command.LocationId);
        var locationResult = await _locationRepository.GetActiveByIdAsync(locationId, cancellationToken);
        if (locationResult.IsFailure)
            return locationResult.Error;

        var nameExistsResult = await _locationRepository.NameExistsAsync(
            nameResult.Value.Value,
            locationId,
            cancellationToken);
        if (nameExistsResult.IsFailure)
            return nameExistsResult.Error;

        if (nameExistsResult.Value)
            return LocationErrors.NameDuplicate(nameof(UpdateLocationCommand.Name));

        locationResult.Value.Update(nameResult.Value, addressResult.Value);

        var saveResult = await _transactionManager.SaveChangesAsync(
            "location",
            new { command.LocationId },
            cancellationToken);
        if (saveResult.IsFailure)
            return saveResult.Error;

        _logger.LogInformation(
            "Location updated successfully. LocationId {LocationId}",
            command.LocationId);

        await _cacheInvalidator.InvalidateAsync(
            [CacheConstants.LOCATIONS_CACHE_TAG, CacheConstants.DEPARTMENTS_CACHE_TAG]);

        return UnitResult.Success<Error>();
    }
}
