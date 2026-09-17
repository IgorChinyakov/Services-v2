using DirectoryService.Contracts.Common;

namespace DirectoryService.Contracts.Locations;

public sealed record LocationDto(
    Guid Id,
    string Name,
    AddressDto Address,
    bool IsActive,
    DateTime CreatedAt,
    long DepartmentCount);
