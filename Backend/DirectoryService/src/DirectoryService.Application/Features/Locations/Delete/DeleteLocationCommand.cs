using DirectoryService.Application.Abstractions.Handlers;

namespace DirectoryService.Application.Features.Locations.Delete;

public sealed record DeleteLocationCommand(Guid LocationId) : ICommand;
