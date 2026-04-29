using System.ComponentModel.DataAnnotations;

namespace DirectoryService.Contracts.Common;

public sealed class AddressDto
{
    [Required]
    public string Country { get; init; } = string.Empty;

    [Required]
    public string City { get; init; } = string.Empty;

    [Required]
    public string Street { get; init; } = string.Empty;

    [Required]
    public string Building { get; init; } = string.Empty;
}
