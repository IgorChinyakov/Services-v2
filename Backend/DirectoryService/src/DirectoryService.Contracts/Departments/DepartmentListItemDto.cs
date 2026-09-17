namespace DirectoryService.Contracts.Departments;

public sealed record DepartmentListItemDto(
    Guid Id,
    string Name,
    string Slug,
    string Path,
    DateTime CreatedAt);
