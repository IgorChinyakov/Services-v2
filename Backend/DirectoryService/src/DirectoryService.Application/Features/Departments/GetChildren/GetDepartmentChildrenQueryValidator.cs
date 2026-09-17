using FluentValidation;

namespace DirectoryService.Application.Features.Departments.GetChildren;

public sealed class GetDepartmentChildrenQueryValidator : AbstractValidator<GetDepartmentChildrenQuery>
{
    public GetDepartmentChildrenQueryValidator()
    {
        RuleFor(query => query.DepartmentId)
            .NotEmpty();
    }
}
