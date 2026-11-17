using DirectoryService.Application.Abstractions.Handlers;

namespace DirectoryService.Application.Features.Search;

public sealed record SearchQuery(string? Q) : IQuery;
