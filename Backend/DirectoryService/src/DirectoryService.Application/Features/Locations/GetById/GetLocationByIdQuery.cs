using DirectoryService.Application.Abstractions.Handlers;

namespace DirectoryService.Application.Features.Locations.GetById;

public sealed record GetLocationByIdQuery(Guid Id) : IQuery;
