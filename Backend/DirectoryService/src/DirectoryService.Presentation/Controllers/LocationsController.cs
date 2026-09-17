using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Features.Locations.Create;
using DirectoryService.Application.Features.Locations.Get;
using DirectoryService.Application.Features.Locations.GetById;
using DirectoryService.Application.Features.Locations.Update;
using DirectoryService.Contracts.Common;
using DirectoryService.Contracts.Locations;
using DirectoryService.Contracts.Locations.Requests;
using DirectoryService.Domain.Shared;
using DirectoryService.Presentation.ApiResponse;
using DirectoryService.Presentation.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Presentation.Controllers;

[ApiController]
[Route("api/locations")]
public sealed class LocationsController : ControllerBase
{
    private readonly ILogger<LocationsController> _logger;

    public LocationsController(ILogger<LocationsController> logger)
    {
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType<EndpointResult<Guid>>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(EndpointResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(EndpointResult), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(EndpointResult), StatusCodes.Status500InternalServerError)]
    public async Task<EndpointResult<Guid>> CreateAsync(
        [FromBody] CreateLocationRequest request,
        [FromServices] ICommandHandler<CreateLocationCommand, Guid> handler,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received create location request. Name {LocationName}. TraceId {TraceId}",
            request.Name,
            HttpContext.TraceIdentifier);

        var command = new CreateLocationCommand(
            request.Name,
            request.Address);

        var result = await handler.Handle(command, cancellationToken);

        return result;
    }

    [HttpPatch("{locationId:guid}")]
    [ProducesResponseType<EndpointResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(EndpointResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(EndpointResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(EndpointResult), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(EndpointResult), StatusCodes.Status500InternalServerError)]
    public async Task<EndpointResult> UpdateAsync(
        Guid locationId,
        [FromBody] UpdateLocationRequest request,
        [FromServices] ICommandHandler<UpdateLocationCommand> handler,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received update location request. LocationId {LocationId}. TraceId {TraceId}",
            locationId,
            HttpContext.TraceIdentifier);

        return await handler.Handle(
            new UpdateLocationCommand(locationId, request.Name, request.Address),
            cancellationToken);
    }

    [HttpGet]
    [ProducesResponseType<EndpointResult<PagedList<LocationDto>>>(StatusCodes.Status200OK)]
    public async Task<EndpointResult<PagedList<LocationDto>>> FetchLocations(
        [FromQuery] GetLocationsRequest request,
        [FromServices] IQueryHandler<GetLocationsQuery, PagedList<LocationDto>> handler,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received get location request. TraceId {TraceId}",
            HttpContext.TraceIdentifier);

        var query = new GetLocationsQuery(
            request.Page,
            request.PageSize,
            request.SortBy,
            request.SortDirection,
            request.DepartmentIds,
            request.Search,
            request.IsActive);

        return await handler.HandleAsync(query, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<EndpointResult<LocationDetailsDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(EndpointResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(EndpointResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(EndpointResult), StatusCodes.Status500InternalServerError)]
    public async Task<EndpointResult<LocationDetailsDto>> GetByIdAsync(
        Guid id,
        [FromServices] IQueryHandler<GetLocationByIdQuery, LocationDetailsDto> handler,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received get location by id request. LocationId {LocationId}. TraceId {TraceId}",
            id,
            HttpContext.TraceIdentifier);

        return await handler.HandleAsync(
            new GetLocationByIdQuery(id),
            cancellationToken);
    }
}
