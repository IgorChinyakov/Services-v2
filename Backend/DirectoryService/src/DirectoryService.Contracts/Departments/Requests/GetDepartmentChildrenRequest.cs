namespace DirectoryService.Contracts.Departments.Requests;

public sealed record GetDepartmentChildrenRequest(
    int Page = 1,
    int Size = 20);
