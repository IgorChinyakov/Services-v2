using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Cache;
using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Application.Extensions.Validation;
using DirectoryService.Domain.Shared;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Application.Features.Positions.CleanupInactive;

public sealed class CleanupInactivePositionsHandler
    : ICommandHandler<CleanupInactivePositionsCommand, int>
{
    private readonly IPositionRepository _positionRepository;
    private readonly IValidator<CleanupInactivePositionsCommand> _validator;
    private readonly ILogger<CleanupInactivePositionsHandler> _logger;
    private readonly ICacheInvalidator _cacheInvalidator;

    public CleanupInactivePositionsHandler(
        IPositionRepository positionRepository,
        IValidator<CleanupInactivePositionsCommand> validator,
        ILogger<CleanupInactivePositionsHandler> logger,
        ICacheInvalidator cacheInvalidator)
    {
        _positionRepository = positionRepository;
        _validator = validator;
        _logger = logger;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<Result<int, Error>> Handle(
        CleanupInactivePositionsCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToError();

        _logger.LogInformation(
            "Starting inactive positions cleanup. DeletedBeforeUtc {DeletedBeforeUtc}. BatchSize {BatchSize}",
            command.DeletedBeforeUtc,
            command.BatchSize);

        var cleanupResult = await _positionRepository.CleanupInactiveAsync(
            command.DeletedBeforeUtc,
            command.BatchSize,
            cancellationToken);

        if (cleanupResult.IsFailure)
        {
            _logger.LogWarning(
                "Inactive positions cleanup failed. DeletedBeforeUtc {DeletedBeforeUtc}. ErrorType {ErrorType}",
                command.DeletedBeforeUtc,
                cleanupResult.Error.Type);

            return cleanupResult.Error;
        }

        _logger.LogInformation(
            "Inactive positions cleanup completed. DeletedCount {DeletedCount}",
            cleanupResult.Value);

        if (cleanupResult.Value > 0)
            await _cacheInvalidator.InvalidateAsync([CacheConstants.DEPARTMENTS_CACHE_TAG]);

        return cleanupResult.Value;
    }
}
