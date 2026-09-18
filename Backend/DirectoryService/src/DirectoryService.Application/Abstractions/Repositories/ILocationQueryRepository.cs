using CSharpFunctionalExtensions;
using DirectoryService.Application.Features.Locations.Get;
using DirectoryService.Contracts.Common;
using DirectoryService.Contracts.Locations;
using DirectoryService.Domain.Shared;

namespace DirectoryService.Application.Abstractions.Repositories;

public interface ILocationQueryRepository
{
    Task<Result<IReadOnlyList<TopLocationByDepartmentsDto>, Error>> GetTopByDepartmentsAsync(
        CancellationToken cancellationToken);

    Task<Result<PagedList<LocationListItemDto>, Error>> GetFilteredWithPaginationAsync(
        GetLocationsQuery query,
        CancellationToken cancellationToken);
}
