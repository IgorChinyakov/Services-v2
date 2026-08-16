using DirectoryService.Application.Abstractions.Handlers;

namespace DirectoryService.Application.Features.Departments.CleanupInactive;

public sealed record CleanupInactiveDepartmentsCommand(
    DateTime DeletedBeforeUtc,
    int BatchSize) : ICommand;
