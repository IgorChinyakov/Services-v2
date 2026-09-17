using DirectoryService.Application.Abstractions.Handlers;

namespace DirectoryService.Application.Features.Departments.RemovePosition;

public sealed record RemoveDepartmentPositionCommand(
    Guid DepartmentId,
    Guid PositionId) : ICommand;
