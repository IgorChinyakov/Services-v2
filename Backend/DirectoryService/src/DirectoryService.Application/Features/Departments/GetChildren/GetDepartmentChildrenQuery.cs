using DirectoryService.Application.Abstractions.Handlers;

namespace DirectoryService.Application.Features.Departments.GetChildren;

public sealed record GetDepartmentChildrenQuery(
    Guid ParentId,
    int Page,
    int Size) : IQuery;
