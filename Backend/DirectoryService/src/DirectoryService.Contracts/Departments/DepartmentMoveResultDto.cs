namespace DirectoryService.Contracts.Departments;

public sealed record DepartmentMoveResultDto(
    Guid Id,
    Guid? ParentId,
    string Path,
    short Depth,
    DateTime UpdatedAt);
