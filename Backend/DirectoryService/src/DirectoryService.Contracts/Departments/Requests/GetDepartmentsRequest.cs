using DirectoryService.Contracts.Departments.Sort;

namespace DirectoryService.Contracts.Departments.Requests;

public sealed record GetDepartmentsRequest(
    string? Search = null,
    DepartmentSortBy SortBy = DepartmentSortBy.Name,
    SortDirection SortDir = SortDirection.Asc,
    int Page = 1,
    int PageSize = 20);
