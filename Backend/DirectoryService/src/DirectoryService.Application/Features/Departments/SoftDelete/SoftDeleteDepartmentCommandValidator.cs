using FluentValidation;

namespace DirectoryService.Application.Features.Departments.SoftDelete;

public sealed class SoftDeleteDepartmentCommandValidator : AbstractValidator<SoftDeleteDepartmentCommand>
{
    public SoftDeleteDepartmentCommandValidator()
    {
        RuleFor(command => command.DepartmentId)
            .NotEmpty()
            .WithMessage("DepartmentId must not be empty");
    }
}
