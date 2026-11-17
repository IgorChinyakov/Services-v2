using DirectoryService.Application.Abstractions.Handlers;

namespace DirectoryService.Application.Features.Departments.Restore;

public sealed record RestoreDepartmentCommand(Guid DepartmentId) : ICommand;
