using DirectoryService.Application.Abstractions.Handlers;

namespace DirectoryService.Application.Features.Departments.Update;

public sealed record UpdateDepartmentCommand(
    Guid DepartmentId,
    string Name) : ICommand;
