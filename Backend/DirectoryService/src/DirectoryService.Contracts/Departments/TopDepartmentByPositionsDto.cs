namespace DirectoryService.Contracts.Departments;

public sealed record TopDepartmentByPositionsDto(
    Guid Id,
    string Name,
    string Identifier,
    long PositionsCount);
