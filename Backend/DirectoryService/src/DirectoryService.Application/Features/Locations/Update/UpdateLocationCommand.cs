using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Contracts.Common;

namespace DirectoryService.Application.Features.Locations.Update;

public sealed record UpdateLocationCommand(
    Guid LocationId,
    string Name,
    AddressDto Address) : ICommand;
