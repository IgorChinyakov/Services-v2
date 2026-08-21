using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Cache;
using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Application.Extensions.Validation;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.Shared;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Application.Features.Departments.UpdateParent;

public sealed class UpdateDepartmentParentHandler
    : ICommandHandler<UpdateDepartmentParentCommand>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IValidator<UpdateDepartmentParentCommand> _validator;
    private readonly ILogger<UpdateDepartmentParentHandler> _logger;
    private readonly ICacheInvalidator _cacheInvalidator;

    public UpdateDepartmentParentHandler(
        IDepartmentRepository departmentRepository,
        IValidator<UpdateDepartmentParentCommand> validator,
        ILogger<UpdateDepartmentParentHandler> logger,
        ICacheInvalidator cacheInvalidator)
    {
        _departmentRepository = departmentRepository;
        _validator = validator;
        _logger = logger;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<UnitResult<Error>> Handle(
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

        if (parentId is not null)
        {
            if (departmentId == parentId)
                return GeneralErrors.Conflict("Department id is equal to its parent id");
        }

        var moveParentResult = await _departmentRepository.MoveParentAsync(
            departmentId,
            parentId,
            cancellationToken);
        if (moveParentResult.IsFailure)
            return moveParentResult.Error;

        await _cacheInvalidator.InvalidateAsync([CacheConstants.DEPARTMENTS_CACHE_TAG]);

        return moveParentResult;
    }
}
