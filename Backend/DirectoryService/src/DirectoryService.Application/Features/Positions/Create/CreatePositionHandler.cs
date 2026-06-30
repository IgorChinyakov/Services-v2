using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Application.Extensions.Validation;
using DirectoryService.Domain.Entities;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.Shared;
using DirectoryService.Domain.ValueObjects.Position;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Application.Features.Positions.Create;

public sealed class CreatePositionHandler
    : ICommandHandler<CreatePositionCommand, Guid>
{
    private readonly IPositionRepository _positionRepository;
    private readonly IValidator<CreatePositionCommand> _validator;
    private readonly ILogger<CreatePositionHandler> _logger;

    public CreatePositionHandler(
        IPositionRepository positionRepository,
        IValidator<CreatePositionCommand> validator,
        ILogger<CreatePositionHandler> logger)
    {
        _positionRepository = positionRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<Guid, Error>> Handle(
        CreatePositionCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting position creation. Name {PositionName}",
            command.Name);

        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning(
                "Position creation validation failed. Errors: {@ValidationErrors}",
                validationResult.Errors.Select(error => new
                {
                    error.PropertyName,
                    error.ErrorMessage,
                }));

            return validationResult.ToError();
        }

        var nameResult = Name.Create(command.Name);
        if (nameResult.IsFailure)
            return nameResult.Error;

        Description? description = null;
        if (!string.IsNullOrWhiteSpace(command.Description))
        {
            var descriptionResult = Description.Create(command.Description);
            if (descriptionResult.IsFailure)
                return descriptionResult.Error;

            description = descriptionResult.Value;
        }

        var activeNameExistsResult = await _positionRepository.ActiveNameExistsAsync(
            nameResult.Value.Value,
            cancellationToken);
        if (activeNameExistsResult.IsFailure)
            return activeNameExistsResult.Error;

        if (activeNameExistsResult.Value)
        {
            return GeneralErrors.Conflict(
                "Active position with the same name already exists.",
                nameof(CreatePositionCommand.Name));
        }

        var departmentIds = command.DepartmentIds
            .Distinct()
            .Select(DepartmentId.Create)
            .ToArray();

        var missingDepartmentsResult = await _positionRepository.GetMissingActiveDepartmentIdsAsync(
            departmentIds,
            cancellationToken);
        if (missingDepartmentsResult.IsFailure)
            return missingDepartmentsResult.Error;

        if (missingDepartmentsResult.Value.Count > 0)
        {
            var missingIds = string.Join(", ", missingDepartmentsResult.Value.Select(id => id.Value));
            return GeneralErrors.Validation(
                $"Departments do not exist or are inactive: {missingIds}",
                nameof(CreatePositionCommand.DepartmentIds));
        }

        var position = new Position(
            nameResult.Value,
            description,
            departmentIds);

        var saveResult = await _positionRepository.AddAsync(position, cancellationToken);
        if (saveResult.IsFailure)
        {
            _logger.LogWarning(
                "Position creation failed. ErrorType {ErrorType}. Errors: {@ErrorMessages}",
                saveResult.Error.Type,
                saveResult.Error.Messages);

            return saveResult.Error;
        }

        _logger.LogInformation(
            "Position created successfully with id {PositionId}",
            saveResult.Value.Id.Value);

        return saveResult.Value.Id.Value;
    }
}
