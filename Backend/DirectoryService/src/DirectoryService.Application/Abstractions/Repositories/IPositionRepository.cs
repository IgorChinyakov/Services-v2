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

    Task<Result<bool, Error>> ActiveNameExistsAsync(
        string name,
        PositionId excludedPositionId,
        CancellationToken cancellationToken);

    Task<Result<Position, Error>> GetActiveByIdAsync(
        PositionId positionId,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyCollection<DepartmentId>, Error>> GetMissingActiveDepartmentIdsAsync(
        IReadOnlyCollection<DepartmentId> departmentIds,
        CancellationToken cancellationToken);

    Task<Result<Position, Error>> AddAsync(
        Position position,
        CancellationToken cancellationToken);

    UnitResult<Error> Delete(Position position);

    Task<Result<int, Error>> CleanupInactiveAsync(
        DateTime deletedBeforeUtc,
        int batchSize,
        CancellationToken cancellationToken);
}
