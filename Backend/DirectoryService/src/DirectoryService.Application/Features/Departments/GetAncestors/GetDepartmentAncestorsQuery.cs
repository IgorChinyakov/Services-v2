using DirectoryService.Application.Abstractions.Handlers;

namespace DirectoryService.Application.Features.Departments.GetAncestors;

public sealed record GetDepartmentAncestorsQuery(Guid DepartmentId) : IQuery;
