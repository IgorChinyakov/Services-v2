using DirectoryService.Application.Abstractions.Handlers;

namespace DirectoryService.Application.Features.Departments.AddPosition;

public sealed record AddDepartmentPositionCommand(
    Guid DepartmentId,
    Guid PositionId) : ICommand;
