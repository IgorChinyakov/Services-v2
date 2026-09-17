using System.ComponentModel.DataAnnotations;
using DirectoryService.Contracts.Common;

namespace DirectoryService.Contracts.Locations.Requests;

public sealed class UpdateLocationRequest
{
    [Required]
    public string Name { get; init; } = string.Empty;

    [Required]
    public AddressDto Address { get; init; } = null!;
}
