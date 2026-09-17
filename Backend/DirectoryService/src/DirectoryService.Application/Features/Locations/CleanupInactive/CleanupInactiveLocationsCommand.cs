using DirectoryService.Application.Abstractions.Handlers;

namespace DirectoryService.Application.Features.Locations.CleanupInactive;

public sealed record CleanupInactiveLocationsCommand(
    DateTime DeletedBeforeUtc,
    int BatchSize) : ICommand;
