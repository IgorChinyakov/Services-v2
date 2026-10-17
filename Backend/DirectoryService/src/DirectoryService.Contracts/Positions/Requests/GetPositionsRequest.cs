namespace DirectoryService.Contracts.Positions.Requests;

public sealed record GetPositionsRequest(
    string? Cursor = null,
    int Size = 20,
    Guid? DepartmentId = null);
