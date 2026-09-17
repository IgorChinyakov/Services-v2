using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Cache;
using DirectoryService.Application.Abstractions.Database;
using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Application.Extensions.Validation;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.Errors;
using DirectoryService.Domain.Shared;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Application.Features.Departments.AddPosition;

public sealed class AddDepartmentPositionHandler : ICommandHandler<AddDepartmentPositionCommand>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IPositionRepository _positionRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly IValidator<AddDepartmentPositionCommand> _validator;
    private readonly ILogger<AddDepartmentPositionHandler> _logger;
    private readonly ICacheInvalidator _cacheInvalidator;

    public AddDepartmentPositionHandler(
        IDepartmentRepository departmentRepository,
        IPositionRepository positionRepository,
        ITransactionManager transactionManager,
        IValidator<AddDepartmentPositionCommand> validator,
        ILogger<AddDepartmentPositionHandler> logger,
        ICacheInvalidator cacheInvalidator)
    {
        _departmentRepository = departmentRepository;
        _positionRepository = positionRepository;
        _transactionManager = transactionManager;
        _validator = validator;
        _logger = logger;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<UnitResult<Error>> Handle(
        AddDepartmentPositionCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToError();

        var departmentId = DepartmentId.Create(command.DepartmentId);
        var positionId = PositionId.Create(command.PositionId);

        var departmentResult = await _departmentRepository.GetActiveByIdAsync(
            departmentId,
            cancellationToken);
        if (departmentResult.IsFailure)
            return departmentResult.Error;

        var positionResult = await _positionRepository.GetActiveByIdAsync(positionId, cancellationToken);
        if (positionResult.IsFailure)
            return positionResult.Error;

        var linkExistsResult = await _departmentRepository.PositionLinkExistsAsync(
            departmentId,
            positionId,
            cancellationToken);
        if (linkExistsResult.IsFailure)
            return linkExistsResult.Error;

        if (linkExistsResult.Value)
            return DepartmentErrors.PositionAlreadyLinked();

        var addResult = await _departmentRepository.AddPositionLinkAsync(
            departmentId,
            positionId,
            cancellationToken);
        if (addResult.IsFailure)
            return addResult.Error;

        var saveResult = await _transactionManager.SaveChangesAsync(
            "department position",
            new { command.DepartmentId, command.PositionId },
            cancellationToken);
        if (saveResult.IsFailure)
            return saveResult.Error;

        _logger.LogInformation(
            "Position linked to department successfully. DepartmentId {DepartmentId}. PositionId {PositionId}",
            command.DepartmentId,
            command.PositionId);

        await _cacheInvalidator.InvalidateAsync([CacheConstants.DEPARTMENTS_CACHE_TAG]);

        return UnitResult.Success<Error>();
    }
}
