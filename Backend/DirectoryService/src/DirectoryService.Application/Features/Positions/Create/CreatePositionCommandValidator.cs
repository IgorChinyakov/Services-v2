using DirectoryService.Application.Extensions.Validation;
using DirectoryService.Domain.ValueObjects.Position;
using FluentValidation;

namespace DirectoryService.Application.Features.Positions.Create;

public sealed class CreatePositionCommandValidator : AbstractValidator<CreatePositionCommand>
{
    public CreatePositionCommandValidator()
    {
        RuleFor(x => x.Name)
            .MustBeValueObject(
                value => Name.Validate(value ?? string.Empty),
                (context, _) => context.PropertyPath);

        When(
            command => !string.IsNullOrWhiteSpace(command.Description),
            () =>
            {
                RuleFor(x => x.Description)
                    .MustBeValueObject(
                        value => Description.Validate(value ?? string.Empty),
                        (context, _) => context.PropertyPath);
            });

        RuleFor(x => x.DepartmentIds)
            .Custom((departmentIds, context) =>
            {
                if (departmentIds is null || departmentIds.Count == 0)
                {
                    context.AddFailure(context.PropertyPath, "DepartmentIds must not be empty");
                    return;
                }

                if (departmentIds.Any(id => id == Guid.Empty))
                    context.AddFailure(context.PropertyPath, "DepartmentIds must not contain empty id");

                if (departmentIds.Distinct().Count() != departmentIds.Count)
                    context.AddFailure(context.PropertyPath, "DepartmentIds must not contain duplicates");
            });
    }
}
