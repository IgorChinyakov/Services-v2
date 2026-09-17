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

namespace DirectoryService.Application.Features.Departments.RemovePosition;

public sealed class RemoveDepartmentPositionHandler : ICommandHandler<RemoveDepartmentPositionCommand>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IPositionRepository _positionRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly IValidator<RemoveDepartmentPositionCommand> _validator;
    private readonly ILogger<RemoveDepartmentPositionHandler> _logger;
    private readonly ICacheInvalidator _cacheInvalidator;

    public RemoveDepartmentPositionHandler(
        IDepartmentRepository departmentRepository,
        IPositionRepository positionRepository,
        ITransactionManager transactionManager,
        IValidator<RemoveDepartmentPositionCommand> validator,
        ILogger<RemoveDepartmentPositionHandler> logger,
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
        RemoveDepartmentPositionCommand command,
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

        var removeResult = await _departmentRepository.RemovePositionLinkAsync(
            departmentId,
            positionId,
            cancellationToken);
        if (removeResult.IsFailure)
            return removeResult.Error;

        if (!removeResult.Value)
            return DepartmentErrors.PositionNotLinked();

        var saveResult = await _transactionManager.SaveChangesAsync(
            "department position",
            new { command.DepartmentId, command.PositionId },
            cancellationToken);
        if (saveResult.IsFailure)
            return saveResult.Error;

        _logger.LogInformation(
            "Position unlinked from department successfully. DepartmentId {DepartmentId}. PositionId {PositionId}",
            command.DepartmentId,
            command.PositionId);

        await _cacheInvalidator.InvalidateAsync([CacheConstants.DEPARTMENTS_CACHE_TAG]);

        return UnitResult.Success<Error>();
    }
}
