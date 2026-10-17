using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Features.Positions.Create;
using DirectoryService.Application.Features.Positions.Delete;
using DirectoryService.Application.Features.Positions.Get;
using DirectoryService.Application.Features.Positions.Update;
using DirectoryService.Contracts.Positions;
using DirectoryService.Contracts.Positions.Requests;
using DirectoryService.Domain.Shared;
using DirectoryService.Presentation.ApiResponse;
using Microsoft.AspNetCore.Mvc;

namespace DirectoryService.Presentation.Controllers;

[ApiController]
[Route("api/positions")]
public sealed class PositionsController : ControllerBase
{
    private readonly ILogger<PositionsController> _logger;

    public PositionsController(ILogger<PositionsController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType<Envelope<PositionCursorPageDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status500InternalServerError)]
    public async Task<EndpointResult<PositionCursorPageDto>> GetAsync(
        [FromQuery] GetPositionsRequest request,
        [FromServices] IQueryHandler<GetPositionsQuery, PositionCursorPageDto> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetPositionsQuery(request.Cursor, request.Size, request.DepartmentId);

        return await handler.HandleAsync(query, cancellationToken);
    }

    [HttpPost]
    [ProducesResponseType<Envelope<Guid>>(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status500InternalServerError)]
    public async Task<EndpointResult<Guid>> CreateAsync(
        [FromBody] CreatePositionRequest request,
        [FromServices] ICommandHandler<CreatePositionCommand, Guid> handler,
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

        var result = await handler.Handle(command, cancellationToken);

        return EndpointResult<Guid>.Created(result);
    }

    [HttpPatch("{positionId:guid}")]
    [ProducesResponseType<Envelope>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status500InternalServerError)]
    public async Task<EndpointResult> UpdateAsync(
        Guid positionId,
        [FromBody] UpdatePositionRequest request,
        [FromServices] ICommandHandler<UpdatePositionCommand> handler,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received update position request. PositionId {PositionId}. TraceId {TraceId}",
            positionId,
            HttpContext.TraceIdentifier);

        return await handler.Handle(
            new UpdatePositionCommand(positionId, request.Name),
            cancellationToken);
    }

    [HttpDelete("{positionId:guid}")]
    [ProducesResponseType<Envelope>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status500InternalServerError)]
    public async Task<EndpointResult> DeleteAsync(
        Guid positionId,
        [FromServices] ICommandHandler<DeletePositionCommand> handler,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received delete position request. PositionId {PositionId}. TraceId {TraceId}",
            positionId,
            HttpContext.TraceIdentifier);

        return await handler.Handle(
            new DeletePositionCommand(positionId),
            cancellationToken);
    }
}
