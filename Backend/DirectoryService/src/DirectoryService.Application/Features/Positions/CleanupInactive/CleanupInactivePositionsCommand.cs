using DirectoryService.Application.Abstractions.Handlers;

namespace DirectoryService.Application.Features.Positions.CleanupInactive;

public sealed record CleanupInactivePositionsCommand(
    DateTime DeletedBeforeUtc,
    int BatchSize) : ICommand;
