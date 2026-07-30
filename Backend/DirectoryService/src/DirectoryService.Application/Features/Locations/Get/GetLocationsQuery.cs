using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Contracts.Locations.Sort;

namespace DirectoryService.Application.Features.Locations.Get;

public record GetLocationsQuery(
    int Page,
    int PageSize,
    LocationSortBy SortBy = LocationSortBy.Name,
    SortDirection SortDirection = SortDirection.Asc,
    Guid[]? DepartmentIds = null,
    string? Search = null,
    bool? IsActive = null) : IQuery;
