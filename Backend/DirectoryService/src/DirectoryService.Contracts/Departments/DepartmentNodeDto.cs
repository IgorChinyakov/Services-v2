namespace DirectoryService.Contracts.Departments;

public sealed record DepartmentNodeDto(
    Guid Id,
    Guid? ParentId,
    string Name,
    string Identifier,
    string Path,
    short Depth,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool HasMoreChildren);
