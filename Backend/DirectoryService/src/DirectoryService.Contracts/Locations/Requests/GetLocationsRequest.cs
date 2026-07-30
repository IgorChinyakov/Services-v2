using DirectoryService.Contracts.Locations.Sort;

namespace DirectoryService.Contracts.Locations.Requests;

public record GetLocationsRequest(
    int Page = 1,
    int PageSize = 20,
    LocationSortBy SortBy = LocationSortBy.Name,
    SortDirection SortDirection = SortDirection.Asc,
    Guid[]? DepartmentIds = null,
    string? Search = null,
    bool? IsActive = null);
