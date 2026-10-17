using FluentValidation;

namespace DirectoryService.Application.Features.Positions.Get;

public sealed class GetPositionsQueryValidator : AbstractValidator<GetPositionsQuery>
{
    public GetPositionsQueryValidator()
    {
        RuleFor(query => query.Size)
            .InclusiveBetween(1, 100);

        RuleFor(query => query.Cursor)
            .MaximumLength(512)
            .When(query => query.Cursor is not null);

        RuleFor(query => query.DepartmentId)
            .NotEqual(Guid.Empty)
            .When(query => query.DepartmentId.HasValue);
    }
}
