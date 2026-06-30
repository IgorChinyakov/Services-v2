using CSharpFunctionalExtensions;
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
    private readonly IValidator<UpdateDepartmentLocationsCommand> _validator;
    private readonly ILogger<UpdateDepartmentLocationsHandler> _logger;

    public UpdateDepartmentLocationsHandler(
        IDepartmentRepository departmentRepository,
        IValidator<UpdateDepartmentLocationsCommand> validator,
        ILogger<UpdateDepartmentLocationsHandler> logger)
    {
        _departmentRepository = departmentRepository;
        _validator = validator;
        _logger = logger;
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
                validationResult.Errors.Select(error => new
                {
                    error.PropertyName,
                    error.ErrorMessage,
                }));

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

        var updateResult = await _departmentRepository.ReplaceLocationsAsync(
            departmentId,
            locationIds,
            cancellationToken);
        if (updateResult.IsFailure)
        {
            _logger.LogWarning(
                "Department locations update failed. DepartmentId {DepartmentId}. ErrorType {ErrorType}. Errors: {@ErrorMessages}",
                command.DepartmentId,
                updateResult.Error.Type,
                updateResult.Error.Messages);

            return updateResult.Error;
        }

        _logger.LogInformation(
            "Department locations updated successfully. DepartmentId {DepartmentId}. LocationCount {LocationCount}",
            command.DepartmentId,
            updateResult.Value);

        return UnitResult.Success<Error>();
    }
}
