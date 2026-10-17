using DirectoryService.Domain.ValueObjects.Department;
using FluentValidation;

namespace DirectoryService.Application.Features.Departments.Get;

public sealed class GetDepartmentsQueryValidator : AbstractValidator<GetDepartmentsQuery>
{
    private const int MAX_PAGE_SIZE = 100;

    public GetDepartmentsQueryValidator()
    {
        RuleFor(query => query.Search)
            .MaximumLength(Name.MAX_LENGTH)
            .When(query => query.Search is not null);

        RuleFor(query => query.SortBy)
            .IsInEnum();

        RuleFor(query => query.SortDirection)
            .IsInEnum();

        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, MAX_PAGE_SIZE);

        RuleFor(query => query.ParentId)
            .NotEqual(Guid.Empty)
            .When(query => query.ParentId.HasValue);

        RuleForEach(query => query.LocationIds)
            .NotEqual(Guid.Empty);

        RuleForEach(query => query.ExcludeIds)
            .NotEqual(Guid.Empty);
    }
}
