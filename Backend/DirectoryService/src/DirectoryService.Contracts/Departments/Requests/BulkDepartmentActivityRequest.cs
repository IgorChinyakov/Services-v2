namespace DirectoryService.Contracts.Departments.Requests;

public sealed record BulkDepartmentActivityRequest(Guid[]? Ids, bool? IsActive);
