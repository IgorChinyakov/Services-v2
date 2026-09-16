using DirectoryService.Contracts.Common;

namespace DirectoryService.Contracts.Locations;

public sealed record LocationDetailsDto(
    Guid Id,
    string Name,
    AddressDto Address,
    string Timezone,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);
