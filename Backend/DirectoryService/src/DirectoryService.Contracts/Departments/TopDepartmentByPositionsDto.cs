namespace DirectoryService.Contracts.Departments;

public sealed record TopDepartmentByPositionsDto(
    Guid Id,
    string Name,
    string Slug,
    long PositionsCount);
