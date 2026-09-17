using CSharpFunctionalExtensions;
using DirectoryService.Domain.Entities;
using DirectoryService.Domain.Shared;

namespace DirectoryService.Application.Abstractions.Repositories;

public interface ILocationRepository
{
    Task<Result<bool, Error>> NameExistsAsync(string name, CancellationToken cancellationToken);

    Task<Result<Location, Error>> AddAsync(Location location, CancellationToken cancellationToken);
}
