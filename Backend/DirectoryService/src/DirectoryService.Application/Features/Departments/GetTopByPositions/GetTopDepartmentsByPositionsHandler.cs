using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Contracts.Departments;
using DirectoryService.Domain.Shared;

namespace DirectoryService.Application.Features.Departments.GetTopByPositions;

public sealed class GetTopDepartmentsByPositionsHandler
    : IQueryHandler<GetTopDepartmentsByPositionsQuery, IReadOnlyList<TopDepartmentByPositionsDto>>
{
    private readonly IDepartmentQueryRepository _departmentQueryRepository;

    public GetTopDepartmentsByPositionsHandler(
        IDepartmentQueryRepository departmentQueryRepository)
    {
        _departmentQueryRepository = departmentQueryRepository;
    }

    public Task<Result<IReadOnlyList<TopDepartmentByPositionsDto>, Error>> HandleAsync(
        GetTopDepartmentsByPositionsQuery query,
        CancellationToken cancellationToken)
    {
        return _departmentQueryRepository.GetTopByPositionsAsync(cancellationToken);
    }
}
