using DirectoryService.Contracts.Common;

namespace DirectoryService.Contracts.Locations;

public sealed record LocationDto(
    Guid Id,
    string Name,
    AddressDto Address,
    string Timezone,
    bool IsActive);
