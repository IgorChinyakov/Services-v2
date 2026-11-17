using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Cache;
using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Application.Extensions.Validation;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.Shared;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Application.Features.Departments.Restore;

public sealed class RestoreDepartmentHandler : ICommandHandler<RestoreDepartmentCommand>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IValidator<RestoreDepartmentCommand> _validator;
    private readonly ILogger<RestoreDepartmentHandler> _logger;
    private readonly ICacheInvalidator _cacheInvalidator;

    public RestoreDepartmentHandler(
        IDepartmentRepository departmentRepository,
        IValidator<RestoreDepartmentCommand> validator,
        ILogger<RestoreDepartmentHandler> logger,
        ICacheInvalidator cacheInvalidator)
    {
        _departmentRepository = departmentRepository;
        _validator = validator;
        _logger = logger;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<UnitResult<Error>> Handle(
        RestoreDepartmentCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToError();

        var result = await _departmentRepository.RestoreAsync(
            DepartmentId.Create(command.DepartmentId), cancellationToken);
        if (result.IsFailure)
            return result.Error;

        _logger.LogInformation(
            "Department restored successfully. DepartmentId {DepartmentId}",
            command.DepartmentId);

        await _cacheInvalidator.InvalidateAsync(
            [CacheConstants.DEPARTMENTS_CACHE_TAG, CacheConstants.LOCATIONS_CACHE_TAG]);

        return UnitResult.Success<Error>();
    }
}
