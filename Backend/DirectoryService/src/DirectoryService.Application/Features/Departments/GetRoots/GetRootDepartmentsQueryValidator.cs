using FluentValidation;

namespace DirectoryService.Application.Features.Departments.GetRoots;

public sealed class GetRootDepartmentsQueryValidator : AbstractValidator<GetRootDepartmentsQuery>
{
    private const int MAX_PAGE_SIZE = 100;
    private const int MAX_PREFETCH_SIZE = 100;

    public GetRootDepartmentsQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(query => query.Size)
            .InclusiveBetween(1, MAX_PAGE_SIZE);

        RuleFor(query => query.Prefetch)
            .InclusiveBetween(0, MAX_PREFETCH_SIZE);
    }
}
