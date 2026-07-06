using System.Text.Json.Serialization;

namespace DirectoryService.Contracts.Departments.Requests;

public sealed class UpdateDepartmentParentRequest
{
    [JsonRequired]
    public Guid? ParentId { get; init; }
}
