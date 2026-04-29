using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Locations.Create;
using DirectoryService.Contracts.Locations.Requests;
using DirectoryService.Presentation.ApiResponse;
using DirectoryService.Presentation.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Presentation.Controllers;

[ApiController]
[Route("api/locations")]
public sealed class LocationsController : ControllerBase
{
    private readonly ICommandHandler<CreateLocationCommand, Guid> _createLocationHandler;
    private readonly ILogger<LocationsController> _logger;

    public LocationsController(
        ICommandHandler<CreateLocationCommand, Guid> createLocationHandler,
        ILogger<LocationsController> logger)
    {
        _createLocationHandler = createLocationHandler;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(Envelope<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateLocationRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received create location request. Name {LocationName}. Timezone {Timezone}. TraceId {TraceId}",
            request.Name,
            request.Timezone,
            HttpContext.TraceIdentifier);

        var command = new CreateLocationCommand(
            request.Name,
            request.Address,
            request.Timezone);

        var result = await _createLocationHandler.Handle(command, cancellationToken);

        if (result.IsFailure)
            return result.Error.ToMvcResult();

        return Ok(Envelope.Ok(result.Value));
    }
}