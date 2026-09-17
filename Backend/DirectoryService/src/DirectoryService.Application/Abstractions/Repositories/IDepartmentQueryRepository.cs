using CSharpFunctionalExtensions;
using DirectoryService.Contracts.Departments;
using DirectoryService.Domain.Shared;

namespace DirectoryService.Application.Abstractions.Repositories;

public interface IDepartmentQueryRepository
{
    Task<Result<IReadOnlyList<TopDepartmentByPositionsDto>, Error>> GetTopByPositionsAsync(
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<DepartmentTreeNodeDto>, Error>> GetTreeAsync(
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<DepartmentTreeNodeDto>, Error>> GetChildrenAsync(
        Guid departmentId,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<DepartmentTreeNodeDto>, Error>> GetAncestorsAsync(
        Guid departmentId,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<DepartmentTreeSearchResultDto>, Error>> SearchTreeAsync(
        string search,
        CancellationToken cancellationToken);
}
