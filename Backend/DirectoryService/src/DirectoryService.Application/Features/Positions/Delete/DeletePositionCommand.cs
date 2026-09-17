using DirectoryService.Application.Abstractions.Handlers;

namespace DirectoryService.Application.Features.Positions.Delete;

public sealed record DeletePositionCommand(Guid PositionId) : ICommand;
