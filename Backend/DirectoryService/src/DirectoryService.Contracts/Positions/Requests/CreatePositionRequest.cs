using System.ComponentModel.DataAnnotations;

namespace DirectoryService.Contracts.Positions.Requests;

public sealed class CreatePositionRequest
{
    [Required]
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    [Required]
    public Guid[] DepartmentIds { get; init; } = [];
}
