namespace DirectoryService.Contracts.Departments;

public sealed record DepartmentListItemDto(
    Guid Id,
    string Name,
    string Identifier,
    string Path,
    DateTime CreatedAt);
