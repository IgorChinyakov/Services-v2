using DirectoryService.Application.Extensions.Validation;
using DirectoryService.Domain.ValueObjects.Department;
using FluentValidation;

namespace DirectoryService.Application.Features.Departments.Create;

public sealed class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentCommandValidator()
    {
        RuleFor(x => x.Name)
            .MustBeValueObject(
                value => Name.Validate(value ?? string.Empty),
                (context, _) => context.PropertyPath);

        RuleFor(x => x.Identifier)
            .MustBeValueObject(
                value => Identifier.Validate(value ?? string.Empty),
                (context, _) => context.PropertyPath);

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
