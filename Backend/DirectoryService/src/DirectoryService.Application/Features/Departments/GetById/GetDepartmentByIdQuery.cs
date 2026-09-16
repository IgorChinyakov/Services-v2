using DirectoryService.Application.Abstractions.Handlers;

namespace DirectoryService.Application.Features.Departments.GetById;

public sealed record GetDepartmentByIdQuery(Guid Id) : IQuery;
