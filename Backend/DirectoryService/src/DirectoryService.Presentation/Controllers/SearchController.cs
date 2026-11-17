using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Features.Search;
using DirectoryService.Contracts.Search;
using DirectoryService.Domain.Shared;
using DirectoryService.Presentation.ApiResponse;
using Microsoft.AspNetCore.Mvc;

namespace DirectoryService.Presentation.Controllers;

[ApiController]
[Route("api/search")]
public sealed class SearchController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<Envelope<IReadOnlyList<GlobalSearchResultDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status500InternalServerError)]
    public async Task<EndpointResult<IReadOnlyList<GlobalSearchResultDto>>> GetAsync(
        [FromQuery] GetSearchRequest request,
        [FromServices] IQueryHandler<SearchQuery, IReadOnlyList<GlobalSearchResultDto>> handler,
        CancellationToken cancellationToken)
    {
        return await handler.HandleAsync(new SearchQuery(request.Q), cancellationToken);
    }
}
