using CSharpFunctionalExtensions;
using DirectoryService.Application.Features.Departments.GetChildren;
using DirectoryService.Application.Features.Departments.GetRoots;
using DirectoryService.Contracts.Common;
using DirectoryService.Contracts.Departments;
using DirectoryService.Domain.Shared;

namespace DirectoryService.Application.Abstractions.Repositories;

public interface IDepartmentQueryRepository
{
    Task<Result<IReadOnlyList<TopDepartmentByPositionsDto>, Error>> GetTopByPositionsAsync(
        CancellationToken cancellationToken);

    Task<Result<PagedList<RootDepartmentDto>, Error>> GetRootsAsync(
        GetRootDepartmentsQuery query,
        CancellationToken cancellationToken);

    Task<Result<PagedList<DepartmentNodeDto>, Error>> GetChildrenAsync(
        GetDepartmentChildrenQuery query,
        CancellationToken cancellationToken);
}
