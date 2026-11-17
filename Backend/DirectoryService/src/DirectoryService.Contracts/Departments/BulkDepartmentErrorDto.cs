namespace DirectoryService.Contracts.Departments;

public sealed record BulkDepartmentErrorDto(Guid Id, string Code, string Message);
