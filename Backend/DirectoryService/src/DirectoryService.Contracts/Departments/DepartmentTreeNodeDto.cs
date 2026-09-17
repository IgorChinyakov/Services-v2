namespace DirectoryService.Contracts.Departments;

public sealed record DepartmentTreeNodeDto(
    Guid Id,
    string Name,
    string Slug,
    string Path,
    short Depth,
    bool HasChildren);
