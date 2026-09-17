using FluentValidation;

namespace DirectoryService.Application.Features.Departments.RemovePosition;

public sealed class RemoveDepartmentPositionCommandValidator : AbstractValidator<RemoveDepartmentPositionCommand>
{
    public RemoveDepartmentPositionCommandValidator()
    {
        RuleFor(command => command.DepartmentId)
            .NotEmpty()
            .WithMessage("DepartmentId must not be empty");

        RuleFor(command => command.PositionId)
            .NotEmpty()
            .WithMessage("PositionId must not be empty");
    }
}
