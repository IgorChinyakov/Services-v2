using DirectoryService.Application.Abstractions.Handlers;

namespace DirectoryService.Application.Features.Departments.UpdateActivity;

public sealed record UpdateDepartmentActivityCommand(Guid DepartmentId, bool IsActive) : ICommand;
