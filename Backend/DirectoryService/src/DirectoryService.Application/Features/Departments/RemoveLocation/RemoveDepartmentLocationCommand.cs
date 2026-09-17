using DirectoryService.Application.Abstractions.Handlers;

namespace DirectoryService.Application.Features.Departments.RemoveLocation;

public sealed record RemoveDepartmentLocationCommand(
    Guid DepartmentId,
    Guid LocationId) : ICommand;
