using CSharpFunctionalExtensions;
using DirectoryService.Application.Features.Locations.Get;
using DirectoryService.Contracts.Common;
using DirectoryService.Contracts.Locations;
using DirectoryService.Domain.Entities;
using DirectoryService.Domain.Shared;

namespace DirectoryService.Application.Abstractions.Repositories;

public interface ILocationRepository
{
    Task<Result<Location, Error>> AddAsync(Location location, CancellationToken cancellationToken);

    Task<PagedList<LocationDto>> GetFilteredWithPaginationAsync(
        GetLocationsQuery query,
        CancellationToken cancellationToken);
}
