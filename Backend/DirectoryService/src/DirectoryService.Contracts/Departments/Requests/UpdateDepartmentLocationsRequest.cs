using System.ComponentModel.DataAnnotations;

namespace DirectoryService.Contracts.Departments.Requests;

public sealed class UpdateDepartmentLocationsRequest
{
    [Required]
    public Guid[] LocationIds { get; init; } = [];
}
