using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Features.Positions.Create;
using DirectoryService.Contracts.Positions.Requests;
using DirectoryService.Domain.Shared;
using DirectoryService.Presentation.ApiResponse;
using Microsoft.AspNetCore.Mvc;

namespace DirectoryService.Presentation.Controllers;

[ApiController]
[Route("api/positions")]
public sealed class PositionsController : ControllerBase
{
    private readonly ICommandHandler<CreatePositionCommand, Guid> _createPositionHandler;
    private readonly ILogger<PositionsController> _logger;

    public PositionsController(
        ICommandHandler<CreatePositionCommand, Guid> createPositionHandler,
        ILogger<PositionsController> logger)
    {
        _createPositionHandler = createPositionHandler;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType<EndpointResult<Guid>>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(EndpointResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(EndpointResult), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(EndpointResult), StatusCodes.Status500InternalServerError)]
    public async Task<EndpointResult<Guid>> CreateAsync(
        [FromBody] CreatePositionRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received create position request. Name {PositionName}. TraceId {TraceId}",
            request.Name,
            HttpContext.TraceIdentifier);

        var command = new CreatePositionCommand(
            request.Name,
            request.Description,
            request.DepartmentIds);

        var result = await _createPositionHandler.Handle(command, cancellationToken);

        return result;
    }
}
