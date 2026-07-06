using FluentValidation;

namespace DirectoryService.Application.Features.Departments.UpdateParent;

public sealed class UpdateDepartmentParentCommandValidator : AbstractValidator<UpdateDepartmentParentCommand>
{
    public UpdateDepartmentParentCommandValidator()
    {
        RuleFor(x => x.DepartmentId)
            .NotEmpty()
            .WithMessage("DepartmentId must not be empty");

        RuleFor(x => x.ParentId)
            .Must(parentId => parentId is null || parentId.Value != Guid.Empty)
            .WithMessage("ParentId must not be empty");

        RuleFor(x => x)
            .Must(command => command.ParentId is null || command.ParentId.Value != command.DepartmentId)
            .WithName(nameof(UpdateDepartmentParentCommand.ParentId))
            .WithMessage("ParentId must not be equal to DepartmentId");
    }
}
