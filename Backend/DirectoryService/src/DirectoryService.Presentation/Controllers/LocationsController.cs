using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Features.Locations.Create;
using DirectoryService.Application.Features.Locations.Delete;
using DirectoryService.Application.Features.Locations.Get;
using DirectoryService.Application.Features.Locations.GetById;
using DirectoryService.Application.Features.Locations.GetTopByDepartments;
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
    [ProducesResponseType<Envelope<Guid>>(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status500InternalServerError)]
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

        return EndpointResult<Guid>.Created(result);
    }

    [HttpPatch("{locationId:guid}")]
    [ProducesResponseType<Envelope>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status500InternalServerError)]
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

    [HttpDelete("{locationId:guid}")]
    [ProducesResponseType<Envelope>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status500InternalServerError)]
    public async Task<EndpointResult> DeleteAsync(
        Guid locationId,
        [FromServices] ICommandHandler<DeleteLocationCommand> handler,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received delete location request. LocationId {LocationId}. TraceId {TraceId}",
            locationId,
            HttpContext.TraceIdentifier);

        return await handler.Handle(
            new DeleteLocationCommand(locationId),
            cancellationToken);
    }

    [HttpGet]
    [ProducesResponseType<Envelope<PagedList<LocationListItemDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status500InternalServerError)]
    public async Task<EndpointResult<PagedList<LocationListItemDto>>> FetchLocations(
        [FromQuery] GetLocationsRequest request,
        [FromServices] IQueryHandler<GetLocationsQuery, PagedList<LocationListItemDto>> handler,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received get location request. TraceId {TraceId}",
            HttpContext.TraceIdentifier);

        var query = new GetLocationsQuery(
            request.Page,
            request.PageSize,
            request.SortBy,
            request.SortDir,
            request.DepartmentIds,
            request.Search,
            request.MinDepartmentCount);

        return await handler.HandleAsync(query, cancellationToken);
    }

    [HttpGet("top")]
    [ProducesResponseType<Envelope<IReadOnlyList<TopLocationByDepartmentsDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status500InternalServerError)]
    public async Task<EndpointResult<IReadOnlyList<TopLocationByDepartmentsDto>>> GetTopAsync(
        [FromServices]
        IQueryHandler<GetTopLocationsByDepartmentsQuery, IReadOnlyList<TopLocationByDepartmentsDto>> handler,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received get top locations by departments request. TraceId {TraceId}",
            HttpContext.TraceIdentifier);

        return await handler.HandleAsync(
            new GetTopLocationsByDepartmentsQuery(),
            cancellationToken);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<Envelope<LocationDetailsDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status500InternalServerError)]
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
