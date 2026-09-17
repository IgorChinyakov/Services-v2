using System.ComponentModel.DataAnnotations;

namespace DirectoryService.Contracts.Common;

public sealed class AddressDto
{
    [Required]
    public string City { get; init; } = string.Empty;

    [Required]
    public string Street { get; init; } = string.Empty;

    [Required]
    public string House { get; init; } = string.Empty;

    [Required]
    public string Apartment { get; init; } = string.Empty;
}
