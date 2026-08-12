using DirectoryService.Application.Abstractions.Handlers;

namespace DirectoryService.Application.Features.Departments.SoftDelete;

public sealed record SoftDeleteDepartmentCommand(Guid DepartmentId) : ICommand;
