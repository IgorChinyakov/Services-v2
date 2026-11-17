using DirectoryService.Application.Abstractions.Handlers;

namespace DirectoryService.Application.Features.Positions.GetById;

public sealed record GetPositionByIdQuery(Guid Id) : IQuery;
