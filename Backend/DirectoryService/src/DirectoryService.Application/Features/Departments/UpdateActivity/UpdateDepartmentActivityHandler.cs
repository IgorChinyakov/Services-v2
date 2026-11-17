using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Cache;
using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Application.Extensions.Validation;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.Shared;
using FluentValidation;

namespace DirectoryService.Application.Features.Departments.UpdateActivity;

public sealed class UpdateDepartmentActivityHandler(
    IDepartmentRepository repository,
    IValidator<UpdateDepartmentActivityCommand> validator,
    ICacheInvalidator cacheInvalidator) : ICommandHandler<UpdateDepartmentActivityCommand>
{
    public async Task<UnitResult<Error>> Handle(
        UpdateDepartmentActivityCommand command,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return validation.ToError();

        var result = await repository.UpdateActivityAsync(
            DepartmentId.Create(command.DepartmentId), command.IsActive, cancellationToken);
        if (result.IsFailure)
            return result.Error;

        await cacheInvalidator.InvalidateAsync(
            [CacheConstants.DEPARTMENTS_CACHE_TAG, CacheConstants.LOCATIONS_CACHE_TAG]);

        return UnitResult.Success<Error>();
    }
}
