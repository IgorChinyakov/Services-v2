using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Cache;
using DirectoryService.Application.Abstractions.Database;
using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Application.Extensions.Validation;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.Shared;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Application.Features.Locations.Delete;

public sealed class DeleteLocationHandler : ICommandHandler<DeleteLocationCommand>
{
    private readonly ILocationRepository _locationRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly IValidator<DeleteLocationCommand> _validator;
    private readonly ILogger<DeleteLocationHandler> _logger;
    private readonly ICacheInvalidator _cacheInvalidator;

    public DeleteLocationHandler(
        ILocationRepository locationRepository,
        ITransactionManager transactionManager,
        IValidator<DeleteLocationCommand> validator,
        ILogger<DeleteLocationHandler> logger,
        ICacheInvalidator cacheInvalidator)
    {
        _locationRepository = locationRepository;
        _transactionManager = transactionManager;
        _validator = validator;
        _logger = logger;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<UnitResult<Error>> Handle(
        DeleteLocationCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToError();

        var locationResult = await _locationRepository.GetActiveByIdAsync(
            LocationId.Create(command.LocationId),
            cancellationToken);
        if (locationResult.IsFailure)
            return locationResult.Error;

        var deleteResult = _locationRepository.Delete(locationResult.Value);
        if (deleteResult.IsFailure)
            return deleteResult.Error;

        var saveResult = await _transactionManager.SaveChangesAsync(
            "location",
            new { command.LocationId },
            cancellationToken);
        if (saveResult.IsFailure)
            return saveResult.Error;

        _logger.LogInformation(
            "Location deleted successfully. LocationId {LocationId}",
            command.LocationId);

        await _cacheInvalidator.InvalidateAsync(
            [CacheConstants.LOCATIONS_CACHE_TAG, CacheConstants.DEPARTMENTS_CACHE_TAG]);

        return UnitResult.Success<Error>();
    }
}
