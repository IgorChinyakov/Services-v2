using System.ComponentModel.DataAnnotations;

namespace DirectoryService.Contracts.Departments.Requests;

public sealed class CreateDepartmentRequest
{
    [Required]
    public string Name { get; init; } = string.Empty;

    [Required]
    public string Identifier { get; init; } = string.Empty;

    public Guid? ParentId { get; init; }

    [Required]
    public Guid[] LocationIds { get; init; } = [];
}
