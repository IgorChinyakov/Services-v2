using FluentValidation;

namespace DirectoryService.Application.Features.Search;

public sealed class SearchQueryValidator : AbstractValidator<SearchQuery>
{
    public SearchQueryValidator()
    {
        RuleFor(query => query.Q)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(200);
    }
}
