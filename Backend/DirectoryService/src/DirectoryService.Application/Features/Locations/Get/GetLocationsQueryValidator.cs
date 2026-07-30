using DirectoryService.Domain.ValueObjects.Location;
using FluentValidation;

namespace DirectoryService.Application.Features.Locations.Get;

public class GetLocationsQueryValidator : AbstractValidator<GetLocationsQuery>
{
    private const int MAX_PAGE_SIZE = 100;
    private const int MAX_DEPARTMENT_IDS = 100;

    public GetLocationsQueryValidator()
    {
        RuleFor(x => x.SortBy).IsInEnum();
        RuleFor(x => x.SortDirection).IsInEnum();

        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, MAX_PAGE_SIZE);

        RuleFor(query => query.Search)
            .MaximumLength(Name.MAX_LENGTH)
            .When(query => query.Search is not null);

        RuleFor(query => query.DepartmentIds)
            .Custom((departmentIds, context) =>
            {
                if (departmentIds is null)
                    return;

                if (departmentIds.Length == 0)
                {
                    context.AddFailure(
                        context.PropertyPath,
                        "DepartmentIds must not be empty");
                    return;
                }

                if (departmentIds.Length > MAX_DEPARTMENT_IDS)
                {
                    context.AddFailure(
                        context.PropertyPath,
                        $"DepartmentIds must contain no more than {MAX_DEPARTMENT_IDS} ids");
                }

                if (departmentIds.Any(id => id == Guid.Empty))
                {
                    context.AddFailure(
                        context.PropertyPath,
                        "DepartmentIds must not contain empty id");
                }

                if (departmentIds.Distinct().Count() != departmentIds.Length)
                {
                    context.AddFailure(
                        context.PropertyPath,
                        "DepartmentIds must not contain duplicates");
                }
            });
    }
}
