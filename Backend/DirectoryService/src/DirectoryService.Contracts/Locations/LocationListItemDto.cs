using DirectoryService.Contracts.Common;

namespace DirectoryService.Contracts.Locations;

public sealed record LocationListItemDto(
    Guid Id,
    string Name,
    AddressDto Address,
    bool IsActive,
    DateTime CreatedAt,
    long DepartmentCount);
