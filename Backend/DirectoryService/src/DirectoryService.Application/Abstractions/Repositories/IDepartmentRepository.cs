using CSharpFunctionalExtensions;
using DirectoryService.Domain.Entities;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.Shared;

namespace DirectoryService.Application.Abstractions.Repositories;

public interface IDepartmentRepository
{
    Task<Result<bool, Error>> SlugExistsAsync(
        string slug,
        CancellationToken cancellationToken);

    Task<Result<Department, Error>> GetActiveByIdAsync(
        DepartmentId departmentId,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyCollection<LocationId>, Error>> GetMissingActiveLocationIdsAsync(
        IReadOnlyCollection<LocationId> locationIds,
        CancellationToken cancellationToken);

    Task<Result<int, Error>> ReplaceLocationsAsync(
        DepartmentId departmentId,
        IReadOnlyCollection<LocationId> locationIds,
        CancellationToken cancellationToken);

    Task<Result<bool, Error>> LocationLinkExistsAsync(
        DepartmentId departmentId,
        LocationId locationId,
        CancellationToken cancellationToken);

    Task<Result<DepartmentLocation, Error>> AddLocationLinkAsync(
        DepartmentId departmentId,
        LocationId locationId,
        CancellationToken cancellationToken);

    Task<Result<bool, Error>> RemoveLocationLinkAsync(
        DepartmentId departmentId,
        LocationId locationId,
        CancellationToken cancellationToken);

    Task<Result<bool, Error>> PositionLinkExistsAsync(
        DepartmentId departmentId,
        PositionId positionId,
        CancellationToken cancellationToken);

    Task<Result<DepartmentPosition, Error>> AddPositionLinkAsync(
        DepartmentId departmentId,
        PositionId positionId,
        CancellationToken cancellationToken);

    Task<Result<bool, Error>> RemovePositionLinkAsync(
        DepartmentId departmentId,
        PositionId positionId,
        CancellationToken cancellationToken);

    Task<Result<DepartmentMoveResult, Error>> MoveParentAsync(
        DepartmentId departmentId,
        DepartmentId? parentId,
        CancellationToken cancellationToken);

    Task<Result<Department, Error>> AddAsync(
        Department department,
        CancellationToken cancellationToken);

    Task<Result<DateTime, Error>> SoftDeleteAsync(
        DepartmentId departmentId,
        CancellationToken cancellationToken);

    Task<Result<int, Error>> CleanupInactiveAsync(
        DateTime deletedBeforeUtc,
        int batchSize,
        CancellationToken cancellationToken);
}
