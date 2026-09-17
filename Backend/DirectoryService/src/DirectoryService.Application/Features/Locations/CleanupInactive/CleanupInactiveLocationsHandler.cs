using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Cache;
using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Application.Extensions.Validation;
using DirectoryService.Domain.Shared;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Application.Features.Locations.CleanupInactive;

public sealed class CleanupInactiveLocationsHandler
    : ICommandHandler<CleanupInactiveLocationsCommand, int>
{
    private readonly ILocationRepository _locationRepository;
    private readonly IValidator<CleanupInactiveLocationsCommand> _validator;
    private readonly ILogger<CleanupInactiveLocationsHandler> _logger;
    private readonly ICacheInvalidator _cacheInvalidator;

    public CleanupInactiveLocationsHandler(
        ILocationRepository locationRepository,
        IValidator<CleanupInactiveLocationsCommand> validator,
        ILogger<CleanupInactiveLocationsHandler> logger,
        ICacheInvalidator cacheInvalidator)
    {
        _locationRepository = locationRepository;
        _validator = validator;
        _logger = logger;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<Result<int, Error>> Handle(
        CleanupInactiveLocationsCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToError();

        _logger.LogInformation(
            "Starting inactive locations cleanup. DeletedBeforeUtc {DeletedBeforeUtc}. BatchSize {BatchSize}",
            command.DeletedBeforeUtc,
            command.BatchSize);

        var cleanupResult = await _locationRepository.CleanupInactiveAsync(
            command.DeletedBeforeUtc,
            command.BatchSize,
            cancellationToken);

        if (cleanupResult.IsFailure)
        {
            _logger.LogWarning(
                "Inactive locations cleanup failed. DeletedBeforeUtc {DeletedBeforeUtc}. ErrorType {ErrorType}",
                command.DeletedBeforeUtc,
                cleanupResult.Error.Type);

            return cleanupResult.Error;
        }

        _logger.LogInformation(
            "Inactive locations cleanup completed. DeletedCount {DeletedCount}",
            cleanupResult.Value);

        if (cleanupResult.Value > 0)
        {
            await _cacheInvalidator.InvalidateAsync(
                [CacheConstants.LOCATIONS_CACHE_TAG, CacheConstants.DEPARTMENTS_CACHE_TAG]);
        }

        return cleanupResult.Value;
    }
}
