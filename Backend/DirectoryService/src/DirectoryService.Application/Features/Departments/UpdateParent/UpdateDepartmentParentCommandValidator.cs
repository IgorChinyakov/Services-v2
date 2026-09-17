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

    }
}
