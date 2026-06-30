using CSharpFunctionalExtensions;
using DirectoryService.Domain.Entities;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.Shared;

namespace DirectoryService.Application.Abstractions.Repositories;

public interface IPositionRepository
{
    Task<Result<bool, Error>> ActiveNameExistsAsync(
        string name,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyCollection<DepartmentId>, Error>> GetMissingActiveDepartmentIdsAsync(
        IReadOnlyCollection<DepartmentId> departmentIds,
        CancellationToken cancellationToken);

    Task<Result<Position, Error>> AddAsync(
        Position position,
        CancellationToken cancellationToken);
}
