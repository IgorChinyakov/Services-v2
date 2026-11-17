using FluentValidation;

namespace DirectoryService.Application.Features.Departments.UpdateActivity;

public sealed class UpdateDepartmentActivityCommandValidator : AbstractValidator<UpdateDepartmentActivityCommand>
{
    public UpdateDepartmentActivityCommandValidator()
    {
        RuleFor(command => command.DepartmentId).NotEmpty();
    }
}
