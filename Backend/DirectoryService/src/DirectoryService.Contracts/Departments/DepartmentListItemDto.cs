namespace DirectoryService.Contracts.Departments;

public sealed record DepartmentListItemDto(
    Guid Id,
    string Name,
    string Slug,
    string Path,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? DeletedAt);
