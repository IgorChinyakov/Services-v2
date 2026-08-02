using FluentValidation;

namespace DirectoryService.Application.Features.Departments.GetChildren;

public sealed class GetDepartmentChildrenQueryValidator : AbstractValidator<GetDepartmentChildrenQuery>
{
    private const int MAX_PAGE_SIZE = 100;

    public GetDepartmentChildrenQueryValidator()
    {
        RuleFor(query => query.ParentId)
            .NotEmpty();

        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(query => query.Size)
            .InclusiveBetween(1, MAX_PAGE_SIZE);
    }
}
