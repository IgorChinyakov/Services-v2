using System.ComponentModel.DataAnnotations;
using DirectoryService.Contracts.Common;

namespace DirectoryService.Contracts.Locations.Requests;

public sealed class CreateLocationRequest
{
    [Required]
    public string Name { get; init; } = string.Empty;

    [Required]
    public AddressDto Address { get; init; } = null!;

    [Required]
    public string Timezone { get; init; } = string.Empty;
}
