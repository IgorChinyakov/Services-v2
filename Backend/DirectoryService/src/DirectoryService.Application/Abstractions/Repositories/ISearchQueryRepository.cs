using CSharpFunctionalExtensions;
using DirectoryService.Contracts.Search;
using DirectoryService.Domain.Shared;

namespace DirectoryService.Application.Abstractions.Repositories;

public interface ISearchQueryRepository
{
    Task<Result<IReadOnlyList<GlobalSearchResultDto>, Error>> SearchAsync(
        string search,
        CancellationToken cancellationToken);
}
