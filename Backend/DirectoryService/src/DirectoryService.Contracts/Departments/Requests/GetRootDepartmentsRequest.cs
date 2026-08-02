namespace DirectoryService.Contracts.Departments.Requests;

public sealed record GetRootDepartmentsRequest(
    int Page = 1,
    int Size = 20,
    int Prefetch = 3);
