using FluentValidation;

namespace DirectoryService.Application.Features.Departments.RemoveLocation;

public sealed class RemoveDepartmentLocationCommandValidator : AbstractValidator<RemoveDepartmentLocationCommand>
{
    public RemoveDepartmentLocationCommandValidator()
    {
        RuleFor(command => command.DepartmentId)
            .NotEmpty()
            .WithMessage("DepartmentId must not be empty");

        RuleFor(command => command.LocationId)
            .NotEmpty()
            .WithMessage("LocationId must not be empty");
    }
}
