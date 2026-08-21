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

namespace DirectoryService.Application.Features.Departments.UpdateLocations;

public sealed class UpdateDepartmentLocationsHandler
    : ICommandHandler<UpdateDepartmentLocationsCommand>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly IValidator<UpdateDepartmentLocationsCommand> _validator;
    private readonly ILogger<UpdateDepartmentLocationsHandler> _logger;
    private readonly ICacheInvalidator _cacheInvalidator;

    public UpdateDepartmentLocationsHandler(
        IDepartmentRepository departmentRepository,
        ITransactionManager transactionManager,
        IValidator<UpdateDepartmentLocationsCommand> validator,
        ILogger<UpdateDepartmentLocationsHandler> logger,
        ICacheInvalidator cacheInvalidator)
    {
        _departmentRepository = departmentRepository;
        _transactionManager = transactionManager;
        _validator = validator;
        _logger = logger;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<UnitResult<Error>> Handle(
        UpdateDepartmentLocationsCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting department locations update. DepartmentId {DepartmentId}. LocationCount {LocationCount}",
            command.DepartmentId,
            command.LocationIds.Count);

        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning(
                "Department locations update validation failed. Errors: {@ValidationErrors}",
                validationResult.Errors.Select(error => new { error.PropertyName, error.ErrorMessage, }));

            return validationResult.ToError();
        }

        var departmentId = DepartmentId.Create(command.DepartmentId);

        var departmentResult = await _departmentRepository.GetActiveByIdAsync(
            departmentId,
            cancellationToken);
        if (departmentResult.IsFailure)
            return departmentResult.Error;

        var locationIds = command.LocationIds
            .Distinct()
            .Select(LocationId.Create)
            .ToArray();

        var missingLocationsResult = await _departmentRepository.GetMissingActiveLocationIdsAsync(
            locationIds,
            cancellationToken);
        if (missingLocationsResult.IsFailure)
            return missingLocationsResult.Error;

        if (missingLocationsResult.Value.Count > 0)
        {
            var missingIds = string.Join(", ", missingLocationsResult.Value.Select(id => id.Value));
            return GeneralErrors.Validation(
                $"Locations do not exist or are inactive: {missingIds}",
                nameof(UpdateDepartmentLocationsCommand.LocationIds));
        }

        var transactionResult = await _transactionManager.BeginTransactionAsync(cancellationToken);
        if (transactionResult.IsFailure)
            return transactionResult.Error;

        await using var transaction = transactionResult.Value;

        var updateResult = await _departmentRepository.ReplaceLocationsAsync(
            departmentId,
            locationIds,
            cancellationToken);
        if (updateResult.IsFailure)
        {
            var rollbackResult = await transaction.RollbackAsync(cancellationToken);
            if (rollbackResult.IsFailure)
                return rollbackResult.Error;

            _logger.LogWarning(
                "Department locations update failed. DepartmentId {DepartmentId}. ErrorType {ErrorType}. Errors: {@ErrorMessages}",
                command.DepartmentId,
                updateResult.Error.Type,
                updateResult.Error.Messages);

            return updateResult.Error;
        }

        var saveResult = await _transactionManager.SaveChangesAsync(
            "department locations",
            new { DepartmentId = departmentId.Value, LocationIds = locationIds.Select(id => id.Value), },
            "Department contains duplicate location links.",
            cancellationToken);
        if (saveResult.IsFailure)
        {
            var rollbackResult = await transaction.RollbackAsync(cancellationToken);
            if (rollbackResult.IsFailure)
                return rollbackResult.Error;

            _logger.LogWarning(
                "Department locations update failed while saving. DepartmentId {DepartmentId}. ErrorType {ErrorType}. Errors: {@ErrorMessages}",
                command.DepartmentId,
                saveResult.Error.Type,
                saveResult.Error.Messages);

            return saveResult.Error;
        }

        var commitResult = await transaction.CommitAsync(cancellationToken);
        if (commitResult.IsFailure)
            return commitResult.Error;

        _logger.LogInformation(
            "Department locations updated successfully. DepartmentId {DepartmentId}. LocationCount {LocationCount}",
            command.DepartmentId,
            updateResult.Value);

        await _cacheInvalidator.InvalidateAsync(
            [CacheConstants.LOCATIONS_CACHE_TAG, CacheConstants.DEPARTMENTS_CACHE_TAG]);

        return UnitResult.Success<Error>();
    }
}
