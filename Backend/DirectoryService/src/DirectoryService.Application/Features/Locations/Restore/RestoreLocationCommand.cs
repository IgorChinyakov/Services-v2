using DirectoryService.Application.Abstractions.Handlers;

namespace DirectoryService.Application.Features.Locations.Restore;

public sealed record RestoreLocationCommand(Guid LocationId) : ICommand;
