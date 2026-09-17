using FluentValidation;

namespace DirectoryService.Application.Features.Departments.AddPosition;

public sealed class AddDepartmentPositionCommandValidator : AbstractValidator<AddDepartmentPositionCommand>
{
    public AddDepartmentPositionCommandValidator()
    {
        RuleFor(command => command.DepartmentId)
            .NotEmpty()
            .WithMessage("DepartmentId must not be empty");

        RuleFor(command => command.PositionId)
            .NotEmpty()
            .WithMessage("PositionId must not be empty");
    }
}
