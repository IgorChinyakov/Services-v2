namespace DirectoryService.Contracts.Departments;

public sealed record DepartmentTreeSearchResultDto(
    DepartmentTreeNodeDto Department,
    IReadOnlyList<DepartmentTreeNodeDto> Ancestors);
