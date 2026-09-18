using DirectoryService.Contracts.Common;

namespace DirectoryService.Contracts.Locations;

public sealed record TopLocationByDepartmentsDto(
    Guid Id,
    string Name,
    AddressDto Address,
    long DepartmentCount);
