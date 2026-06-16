using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Contracts.Common;

namespace DirectoryService.Application.Locations.Create;

public sealed record CreateLocationCommand(
    string Name,
    AddressDto Address,
    string Timezone) : ICommand;