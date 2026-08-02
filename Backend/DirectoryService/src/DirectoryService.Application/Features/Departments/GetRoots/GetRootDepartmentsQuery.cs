using DirectoryService.Application.Abstractions.Handlers;

namespace DirectoryService.Application.Features.Departments.GetRoots;

public sealed record GetRootDepartmentsQuery(
    int Page,
    int Size,
    int Prefetch) : IQuery;
