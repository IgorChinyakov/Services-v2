using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Application.Extensions.Validation;
using DirectoryService.Contracts.Search;
using DirectoryService.Domain.Shared;
using FluentValidation;

namespace DirectoryService.Application.Features.Search;

public sealed class SearchHandler(
    ISearchQueryRepository repository,
    IValidator<SearchQuery> validator) : IQueryHandler<SearchQuery, IReadOnlyList<GlobalSearchResultDto>>
{
    public async Task<Result<IReadOnlyList<GlobalSearchResultDto>, Error>> HandleAsync(
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        var normalizedQuery = query with { Q = query.Q?.Trim() ?? string.Empty };
        var validation = await validator.ValidateAsync(normalizedQuery, cancellationToken);
        if (!validation.IsValid)
            return validation.ToError();

        return await repository.SearchAsync(normalizedQuery.Q!, cancellationToken);
    }
}
