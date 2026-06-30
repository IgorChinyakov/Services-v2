using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Application.Extensions.Validation;
using DirectoryService.Domain.Entities;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.Shared;
using DirectoryService.Domain.ValueObjects.Department;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Application.Features.Departments.Create;

public sealed class CreateDepartmentHandler
    : ICommandHandler<CreateDepartmentCommand, Guid>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IValidator<CreateDepartmentCommand> _validator;
    private readonly ILogger<CreateDepartmentHandler> _logger;

    public CreateDepartmentHandler(
        IDepartmentRepository departmentRepository,
        IValidator<CreateDepartmentCommand> validator,
        ILogger<CreateDepartmentHandler> logger)
    {
        _departmentRepository = departmentRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<Guid, Error>> Handle(
        CreateDepartmentCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting department creation. Identifier {DepartmentIdentifier}. ParentId {ParentId}",
            command.Identifier,
            command.ParentId);

        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning(
                "Department creation validation failed. Errors: {@ValidationErrors}",
                validationResult.Errors.Select(error => new
                {
                    error.PropertyName,
                    error.ErrorMessage,
                }));

            return validationResult.ToError();
        }

        var nameResult = Name.Create(command.Name);
        var identifierResult = Identifier.Create(command.Identifier);

        if (nameResult.IsFailure)
            return nameResult.Error;

        if (identifierResult.IsFailure)
            return identifierResult.Error;

        var identifierExistsResult = await _departmentRepository.IdentifierExistsAsync(
            identifierResult.Value.Value,
            cancellationToken);
        if (identifierExistsResult.IsFailure)
            return identifierExistsResult.Error;

        if (identifierExistsResult.Value)
        {
            return GeneralErrors.Conflict(
                "Department identifier already exists.",
                nameof(CreateDepartmentCommand.Identifier));
        }

        Department? parent = null;
        if (command.ParentId is not null)
        {
            var parentResult = await _departmentRepository.GetActiveByIdAsync(
                DepartmentId.Create(command.ParentId.Value),
                cancellationToken);
            if (parentResult.IsFailure)
                return parentResult.Error;

            parent = parentResult.Value;
        }

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
                nameof(CreateDepartmentCommand.LocationIds));
        }

        var department = new Department(
            nameResult.Value,
            identifierResult.Value,
            parent,
            locationIds);

        var saveResult = await _departmentRepository.AddAsync(department, cancellationToken);
        if (saveResult.IsFailure)
        {
            _logger.LogWarning(
                "Department creation failed. ErrorType {ErrorType}. Errors: {@ErrorMessages}",
                saveResult.Error.Type,
                saveResult.Error.Messages);

            return saveResult.Error;
        }

        _logger.LogInformation(
            "Department created successfully with id {DepartmentId}. Path {DepartmentPath}. Depth {DepartmentDepth}",
            saveResult.Value.Id.Value,
            saveResult.Value.Path,
            saveResult.Value.Depth);

        return saveResult.Value.Id.Value;
    }
}
