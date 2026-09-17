using DirectoryService.Application.Extensions.Validation;
using DirectoryService.Domain.ValueObjects.Department;
using FluentValidation;

namespace DirectoryService.Application.Features.Departments.Update;

public sealed class UpdateDepartmentCommandValidator : AbstractValidator<UpdateDepartmentCommand>
{
    public UpdateDepartmentCommandValidator()
    {
        RuleFor(command => command.DepartmentId)
            .NotEmpty()
            .WithMessage("DepartmentId must not be empty");

        RuleFor(command => command.Name)
            .MustBeValueObject(
                value => Name.Validate(value ?? string.Empty),
                (context, _) => context.PropertyPath);
    }
}
