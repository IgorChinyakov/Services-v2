using CSharpFunctionalExtensions;
using DirectoryService.Domain.Entities;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.Shared;

namespace DirectoryService.Application.Abstractions.Repositories;

public interface ILocationRepository
{
    Task<Result<bool, Error>> NameExistsAsync(string name, CancellationToken cancellationToken);

    Task<Result<bool, Error>> NameExistsAsync(
        string name,
        LocationId excludedLocationId,
        CancellationToken cancellationToken);

    Task<Result<Location, Error>> GetActiveByIdAsync(
        LocationId locationId,
        CancellationToken cancellationToken);

    Task<Result<Location, Error>> AddAsync(Location location, CancellationToken cancellationToken);
}
