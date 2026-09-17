using FluentValidation;

namespace DirectoryService.Application.Features.Departments.GetAncestors;

public sealed class GetDepartmentAncestorsQueryValidator : AbstractValidator<GetDepartmentAncestorsQuery>
{
    public GetDepartmentAncestorsQueryValidator()
    {
        RuleFor(query => query.DepartmentId)
            .NotEmpty();
    }
}
