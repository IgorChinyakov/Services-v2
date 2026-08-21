using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Cache;
using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Application.Extensions.Validation;
using DirectoryService.Domain.Shared;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Application.Features.Departments.CleanupInactive;

public sealed class CleanupInactiveDepartmentsHandler
    : ICommandHandler<CleanupInactiveDepartmentsCommand, int>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IValidator<CleanupInactiveDepartmentsCommand> _validator;
    private readonly ILogger<CleanupInactiveDepartmentsHandler> _logger;
    private readonly ICacheInvalidator _cacheInvalidator;

    public CleanupInactiveDepartmentsHandler(
        IDepartmentRepository departmentRepository,
        IValidator<CleanupInactiveDepartmentsCommand> validator,
        ILogger<CleanupInactiveDepartmentsHandler> logger,
        ICacheInvalidator cacheInvalidator)
    {
        _departmentRepository = departmentRepository;
        _validator = validator;
        _logger = logger;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<Result<int, Error>> Handle(
        CleanupInactiveDepartmentsCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToError();

        _logger.LogInformation(
            "Starting inactive departments cleanup. DeletedBeforeUtc {DeletedBeforeUtc}. BatchSize {BatchSize}",
            command.DeletedBeforeUtc,
            command.BatchSize);

        var cleanupResult = await _departmentRepository.CleanupInactiveAsync(
            command.DeletedBeforeUtc,
            command.BatchSize,
            cancellationToken);

        if (cleanupResult.IsFailure)
        {
            _logger.LogWarning(
                "Inactive departments cleanup failed. DeletedBeforeUtc {DeletedBeforeUtc}. ErrorType {ErrorType}",
                command.DeletedBeforeUtc,
                cleanupResult.Error.Type);

            return cleanupResult.Error;
        }

        _logger.LogInformation(
            "Inactive departments cleanup completed. DeletedCount {DeletedCount}",
            cleanupResult.Value);

        await _cacheInvalidator.InvalidateAsync(
            [CacheConstants.LOCATIONS_CACHE_TAG, CacheConstants.DEPARTMENTS_CACHE_TAG]);

        return cleanupResult.Value;
    }
}
