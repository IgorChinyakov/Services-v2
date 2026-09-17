using DirectoryService.Domain.ValueObjects.Department;
using FluentValidation;

namespace DirectoryService.Application.Features.Departments.SearchTree;

public sealed class SearchDepartmentTreeQueryValidator : AbstractValidator<SearchDepartmentTreeQuery>
{
    public SearchDepartmentTreeQueryValidator()
    {
        RuleFor(query => query.Search)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(Name.MAX_LENGTH);
    }
}
