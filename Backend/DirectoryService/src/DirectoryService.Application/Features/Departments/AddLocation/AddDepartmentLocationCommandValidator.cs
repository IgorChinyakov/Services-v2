using FluentValidation;

namespace DirectoryService.Application.Features.Departments.AddLocation;

public sealed class AddDepartmentLocationCommandValidator : AbstractValidator<AddDepartmentLocationCommand>
{
    public AddDepartmentLocationCommandValidator()
    {
        RuleFor(command => command.DepartmentId)
            .NotEmpty()
            .WithMessage("DepartmentId must not be empty");

        RuleFor(command => command.LocationId)
            .NotEmpty()
            .WithMessage("LocationId must not be empty");
    }
}
