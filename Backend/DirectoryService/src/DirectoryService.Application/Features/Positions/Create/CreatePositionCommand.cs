using DirectoryService.Application.Abstractions.Handlers;

namespace DirectoryService.Application.Features.Positions.Create;

public sealed record CreatePositionCommand(
    string Name,
    string? Description,
    IReadOnlyCollection<Guid> DepartmentIds) : ICommand;
