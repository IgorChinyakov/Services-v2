using FluentValidation;

namespace DirectoryService.Application.Features.Departments.Restore;

public sealed class RestoreDepartmentCommandValidator : AbstractValidator<RestoreDepartmentCommand>
{
    public RestoreDepartmentCommandValidator()
    {
        RuleFor(command => command.DepartmentId)
            .NotEmpty()
            .WithMessage("DepartmentId must not be empty");
    }
}
