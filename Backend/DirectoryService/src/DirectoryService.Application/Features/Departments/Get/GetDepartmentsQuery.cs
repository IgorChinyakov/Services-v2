using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Contracts.Departments.Sort;

namespace DirectoryService.Application.Features.Departments.Get;

public sealed record GetDepartmentsQuery(
    string? Search,
    DepartmentSortBy SortBy,
    SortDirection SortDirection,
    int Page,
    int PageSize,
    bool? IsActive = null,
    Guid? ParentId = null,
    Guid[]? LocationIds = null,
    Guid[]? ExcludeIds = null) : IQuery;
