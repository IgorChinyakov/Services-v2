using FluentValidation;

namespace DirectoryService.Application.Features.Departments.UpdateLocations;

public sealed class UpdateDepartmentLocationsCommandValidator : AbstractValidator<UpdateDepartmentLocationsCommand>
{
    public UpdateDepartmentLocationsCommandValidator()
    {
        RuleFor(x => x.DepartmentId)
            .NotEmpty()
            .WithMessage("DepartmentId must not be empty");

        RuleFor(x => x.LocationIds)
            .Custom((locationIds, context) =>
            {
                if (locationIds is null || locationIds.Count == 0)
                {
                    context.AddFailure(context.PropertyPath, "LocationIds must not be empty");
                    return;
                }

                if (locationIds.Any(id => id == Guid.Empty))
                    context.AddFailure(context.PropertyPath, "LocationIds must not contain empty id");

                if (locationIds.Distinct().Count() != locationIds.Count)
                    context.AddFailure(context.PropertyPath, "LocationIds must not contain duplicates");
            });
    }
}
