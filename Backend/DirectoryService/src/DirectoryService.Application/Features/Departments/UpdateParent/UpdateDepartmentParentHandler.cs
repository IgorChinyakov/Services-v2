using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Cache;
using DirectoryService.Application.Abstractions.Database;
using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Application.Extensions.Validation;
using DirectoryService.Contracts.Departments;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.Errors;
using DirectoryService.Domain.Shared;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Application.Features.Departments.UpdateParent;

public sealed class UpdateDepartmentParentHandler
    : ICommandHandler<UpdateDepartmentParentCommand, DepartmentMoveResultDto>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IValidator<UpdateDepartmentParentCommand> _validator;
    private readonly ILogger<UpdateDepartmentParentHandler> _logger;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly ITransactionManager _transactionManager;

    public UpdateDepartmentParentHandler(
        IDepartmentRepository departmentRepository,
        IValidator<UpdateDepartmentParentCommand> validator,
        ILogger<UpdateDepartmentParentHandler> logger,
        ICacheInvalidator cacheInvalidator,
        ITransactionManager transactionManager)
    {
        _departmentRepository = departmentRepository;
        _validator = validator;
        _logger = logger;
        _cacheInvalidator = cacheInvalidator;
        _transactionManager = transactionManager;
    }

    public async Task<Result<DepartmentMoveResultDto, Error>> Handle(
        UpdateDepartmentParentCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting department parent update. DepartmentId {DepartmentId}. ParentId {ParentId}",
            command.DepartmentId,
            command.ParentId);

        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning(
                "Department parent update validation failed. Errors: {@ValidationErrors}",
                validationResult.Errors.Select(error => new
                {
                    error.PropertyName,
                    error.ErrorMessage,
                }));

            return validationResult.ToError();
        }

        var departmentId = DepartmentId.Create(command.DepartmentId);
        var parentId = command.ParentId is null
            ? null
            : DepartmentId.Create(command.ParentId.Value);

        var transactionResult = await _transactionManager.BeginTransactionAsync(cancellationToken);
        if (transactionResult.IsFailure)
            return transactionResult.Error;

        await using var transaction = transactionResult.Value;

        var moveParentResult = await _departmentRepository.MoveParentAsync(
            departmentId,
            parentId,
            cancellationToken);
        if (moveParentResult.IsFailure)
        {
            var rollbackResult = await transaction.RollbackAsync(cancellationToken);
            if (rollbackResult.IsFailure)
                return rollbackResult.Error;

            return moveParentResult.Error;
        }

        var commitResult = await transaction.CommitAsync(cancellationToken);
        if (commitResult.IsFailure)
            return commitResult.Error;

        if (moveParentResult.Value.WasChanged)
            await _cacheInvalidator.InvalidateAsync([CacheConstants.DEPARTMENTS_CACHE_TAG]);

        var result = moveParentResult.Value;

        return new DepartmentMoveResultDto(
            result.Id,
            result.ParentId,
            result.Path,
            result.Depth,
            result.UpdatedAt);
    }
}
