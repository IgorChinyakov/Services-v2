using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Cache;
using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Application.Extensions.Validation;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.Shared;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Application.Features.Departments.SoftDelete;

public sealed class SoftDeleteDepartmentHandler : ICommandHandler<SoftDeleteDepartmentCommand>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IValidator<SoftDeleteDepartmentCommand> _validator;
    private readonly ILogger<SoftDeleteDepartmentHandler> _logger;
    private readonly ICacheInvalidator _cacheInvalidator;

    public SoftDeleteDepartmentHandler(
        IDepartmentRepository departmentRepository,
        IValidator<SoftDeleteDepartmentCommand> validator,
        ILogger<SoftDeleteDepartmentHandler> logger,
        ICacheInvalidator cacheInvalidator)
    {
        _departmentRepository = departmentRepository;
        _validator = validator;
        _logger = logger;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<UnitResult<Error>> Handle(
        SoftDeleteDepartmentCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting department soft delete. DepartmentId {DepartmentId}",
            command.DepartmentId);

        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning(
                "Department soft delete validation failed. DepartmentId {DepartmentId}. Errors: {@ValidationErrors}",
                command.DepartmentId,
                validationResult.Errors.Select(error => new
                {
                    error.PropertyName,
                    error.ErrorMessage,
                }));

            return validationResult.ToError();
        }

        var departmentId = DepartmentId.Create(command.DepartmentId);

        var softDeleteResult = await _departmentRepository.SoftDeleteAsync(departmentId, cancellationToken);

        if (softDeleteResult.IsFailure)
        {
            _logger.LogWarning(
                "Department soft delete failed. DepartmentId {DepartmentId}. ErrorType {ErrorType}",
                command.DepartmentId,
                softDeleteResult.Error.Type);

            return softDeleteResult.Error;
        }

        _logger.LogInformation(
            "Department soft deleted successfully. DepartmentId {DepartmentId}. DeletedAt {DeletedAt}",
            command.DepartmentId,
            softDeleteResult.Value);

        await _cacheInvalidator.InvalidateAsync(
            [CacheConstants.LOCATIONS_CACHE_TAG, CacheConstants.DEPARTMENTS_CACHE_TAG]);

        return UnitResult.Success<Error>();
    }
}
