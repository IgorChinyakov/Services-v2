using System.ComponentModel.DataAnnotations;

namespace DirectoryService.Contracts.Departments.Requests;

public sealed class UpdateDepartmentRequest
{
    [Required]
    public string Name { get; init; } = string.Empty;
}
