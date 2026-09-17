using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Cache;
using DirectoryService.Application.Abstractions.Database;
using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Application.Extensions.Validation;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.Shared;
using DirectoryService.Domain.ValueObjects.Department;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Application.Features.Departments.Update;

public sealed class UpdateDepartmentHandler : ICommandHandler<UpdateDepartmentCommand>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly IValidator<UpdateDepartmentCommand> _validator;
    private readonly ILogger<UpdateDepartmentHandler> _logger;
    private readonly ICacheInvalidator _cacheInvalidator;

    public UpdateDepartmentHandler(
        IDepartmentRepository departmentRepository,
        ITransactionManager transactionManager,
        IValidator<UpdateDepartmentCommand> validator,
        ILogger<UpdateDepartmentHandler> logger,
        ICacheInvalidator cacheInvalidator)
    {
        _departmentRepository = departmentRepository;
        _transactionManager = transactionManager;
        _validator = validator;
        _logger = logger;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<UnitResult<Error>> Handle(
        UpdateDepartmentCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToError();

        var nameResult = Name.Create(command.Name);
        if (nameResult.IsFailure)
            return nameResult.Error;

        var departmentResult = await _departmentRepository.GetActiveByIdAsync(
            DepartmentId.Create(command.DepartmentId),
            cancellationToken);
        if (departmentResult.IsFailure)
            return departmentResult.Error;

        departmentResult.Value.UpdateName(nameResult.Value);

        var saveResult = await _transactionManager.SaveChangesAsync(
            "department",
            new { command.DepartmentId },
            cancellationToken);
        if (saveResult.IsFailure)
            return saveResult.Error;

        _logger.LogInformation(
            "Department updated successfully. DepartmentId {DepartmentId}",
            command.DepartmentId);

        await _cacheInvalidator.InvalidateAsync([CacheConstants.DEPARTMENTS_CACHE_TAG]);

        return UnitResult.Success<Error>();
    }
}
