using System.ComponentModel.DataAnnotations;

namespace DirectoryService.Contracts.Positions.Requests;

public sealed class UpdatePositionRequest
{
    [Required]
    public string Name { get; init; } = string.Empty;
}
