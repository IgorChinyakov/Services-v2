namespace DirectoryService.Contracts.Departments;

public sealed record RootDepartmentDto(
    Guid Id,
    Guid? ParentId,
    string Name,
    string Identifier,
    string Path,
    short Depth,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool HasMoreChildren,
    IReadOnlyList<DepartmentNodeDto> Children);
