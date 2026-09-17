using DirectoryService.Application.Abstractions.Handlers;

namespace DirectoryService.Application.Features.Positions.Update;

public sealed record UpdatePositionCommand(
    Guid PositionId,
    string Name) : ICommand;
