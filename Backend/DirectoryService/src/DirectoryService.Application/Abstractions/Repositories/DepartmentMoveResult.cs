namespace DirectoryService.Application.Abstractions.Repositories;

public sealed record DepartmentMoveResult(
    Guid Id,
    Guid? ParentId,
    string Path,
    short Depth,
    DateTime UpdatedAt,
    bool WasChanged);
