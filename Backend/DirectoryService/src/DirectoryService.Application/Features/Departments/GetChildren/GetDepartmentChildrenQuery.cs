using DirectoryService.Application.Abstractions.Handlers;

namespace DirectoryService.Application.Features.Departments.GetChildren;

public sealed record GetDepartmentChildrenQuery(
    Guid DepartmentId) : IQuery;
