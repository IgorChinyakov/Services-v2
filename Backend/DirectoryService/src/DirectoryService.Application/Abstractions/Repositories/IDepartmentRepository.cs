using CSharpFunctionalExtensions;
using DirectoryService.Domain.Entities;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.Shared;

namespace DirectoryService.Application.Abstractions.Repositories;

public interface IDepartmentRepository
{
    Task<Result<bool, Error>> IdentifierExistsAsync(
        string identifier,
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

    Task<Result<Department, Error>> AddAsync(
        Department department,
        CancellationToken cancellationToken);
}
