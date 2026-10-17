using DirectoryService.Application.Abstractions.Handlers;

namespace DirectoryService.Application.Features.Positions.Get;

public sealed record GetPositionsQuery(
    string? Cursor,
    int Size,
    Guid? DepartmentId) : IQuery;
