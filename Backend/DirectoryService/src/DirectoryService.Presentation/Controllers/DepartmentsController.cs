using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Features.Departments.AddLocation;
using DirectoryService.Application.Features.Departments.AddPosition;
using DirectoryService.Application.Features.Departments.Create;
using DirectoryService.Application.Features.Departments.Get;
using DirectoryService.Application.Features.Departments.GetById;
using DirectoryService.Application.Features.Departments.GetChildren;
using DirectoryService.Application.Features.Departments.GetRoots;
using DirectoryService.Application.Features.Departments.GetTopByPositions;
using DirectoryService.Application.Features.Departments.RemoveLocation;
using DirectoryService.Application.Features.Departments.RemovePosition;
using DirectoryService.Application.Features.Departments.SoftDelete;
using DirectoryService.Application.Features.Departments.Update;
using DirectoryService.Application.Features.Departments.UpdateLocations;
using DirectoryService.Application.Features.Departments.UpdateParent;
using DirectoryService.Contracts.Common;
using DirectoryService.Contracts.Departments;
using DirectoryService.Contracts.Departments.Requests;
using DirectoryService.Domain.Shared;
using DirectoryService.Presentation.ApiResponse;
using Microsoft.AspNetCore.Mvc;

namespace DirectoryService.Presentation.Controllers;

[ApiController]
[Route("api/departments")]
public sealed class DepartmentsController : ControllerBase
{
    private readonly ILogger<DepartmentsController> _logger;

    public DepartmentsController(ILogger<DepartmentsController> logger)
    {
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType<Envelope<Guid>>(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status500InternalServerError)]
    public async Task<EndpointResult<Guid>> CreateAsync(
        [FromBody] CreateDepartmentRequest request,
        [FromServices] ICommandHandler<CreateDepartmentCommand, Guid> handler,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received create department request. Slug {DepartmentSlug}. ParentId {ParentId}. TraceId {TraceId}",
            request.Slug,
            request.ParentId,
            HttpContext.TraceIdentifier);

        var command = new CreateDepartmentCommand(
            request.Name,
            request.Slug,
            request.ParentId,
            request.LocationIds);

        var result = await handler.Handle(command, cancellationToken);

        return EndpointResult<Guid>.Created(result);
    }

    [HttpPatch("{departmentId:guid}")]
    [ProducesResponseType<Envelope>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status500InternalServerError)]
    public async Task<EndpointResult> UpdateAsync(
        Guid departmentId,
        [FromBody] UpdateDepartmentRequest request,
        [FromServices] ICommandHandler<UpdateDepartmentCommand> handler,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received update department request. DepartmentId {DepartmentId}. TraceId {TraceId}",
            departmentId,
            HttpContext.TraceIdentifier);

        return await handler.Handle(
            new UpdateDepartmentCommand(departmentId, request.Name),
            cancellationToken);
    }

    [HttpPost("{departmentId:guid}/locations/{locationId:guid}")]
    [ProducesResponseType<Envelope>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status500InternalServerError)]
    public async Task<EndpointResult> AddLocationAsync(
        Guid departmentId,
        Guid locationId,
        [FromServices] ICommandHandler<AddDepartmentLocationCommand> handler,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received add department location request. DepartmentId {DepartmentId}. LocationId {LocationId}. TraceId {TraceId}",
            departmentId,
            locationId,
            HttpContext.TraceIdentifier);

        return await handler.Handle(
            new AddDepartmentLocationCommand(departmentId, locationId),
            cancellationToken);
    }

    [HttpDelete("{departmentId:guid}/locations/{locationId:guid}")]
    [ProducesResponseType<Envelope>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status500InternalServerError)]
    public async Task<EndpointResult> RemoveLocationAsync(
        Guid departmentId,
        Guid locationId,
        [FromServices] ICommandHandler<RemoveDepartmentLocationCommand> handler,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received remove department location request. DepartmentId {DepartmentId}. LocationId {LocationId}. TraceId {TraceId}",
            departmentId,
            locationId,
            HttpContext.TraceIdentifier);

        return await handler.Handle(
            new RemoveDepartmentLocationCommand(departmentId, locationId),
            cancellationToken);
    }

    [HttpPost("{departmentId:guid}/positions/{positionId:guid}")]
    [ProducesResponseType<Envelope>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status500InternalServerError)]
    public async Task<EndpointResult> AddPositionAsync(
        Guid departmentId,
        Guid positionId,
        [FromServices] ICommandHandler<AddDepartmentPositionCommand> handler,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received add department position request. DepartmentId {DepartmentId}. PositionId {PositionId}. TraceId {TraceId}",
            departmentId,
            positionId,
            HttpContext.TraceIdentifier);

        return await handler.Handle(
            new AddDepartmentPositionCommand(departmentId, positionId),
            cancellationToken);
    }

    [HttpDelete("{departmentId:guid}/positions/{positionId:guid}")]
    [ProducesResponseType<Envelope>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status500InternalServerError)]
    public async Task<EndpointResult> RemovePositionAsync(
        Guid departmentId,
        Guid positionId,
        [FromServices] ICommandHandler<RemoveDepartmentPositionCommand> handler,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received remove department position request. DepartmentId {DepartmentId}. PositionId {PositionId}. TraceId {TraceId}",
            departmentId,
            positionId,
            HttpContext.TraceIdentifier);

        return await handler.Handle(
            new RemoveDepartmentPositionCommand(departmentId, positionId),
            cancellationToken);
    }

    [HttpGet]
    [ProducesResponseType<Envelope<PagedList<DepartmentListItemDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status500InternalServerError)]
    public async Task<EndpointResult<PagedList<DepartmentListItemDto>>> GetAsync(
        [FromQuery] GetDepartmentsRequest request,
        [FromServices] IQueryHandler<GetDepartmentsQuery, PagedList<DepartmentListItemDto>> handler,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received get departments request. Search {Search}. SortBy {SortBy}. SortDirection {SortDirection}. " +
            "Page {Page}. PageSize {PageSize}. TraceId {TraceId}",
            request.Search,
            request.SortBy,
            request.SortDir,
            request.Page,
            request.PageSize,
            HttpContext.TraceIdentifier);

        var query = new GetDepartmentsQuery(
            request.Search,
            request.SortBy,
            request.SortDir,
            request.Page,
            request.PageSize);

        return await handler.HandleAsync(query, cancellationToken);
    }

    [HttpGet("roots")]
    [ProducesResponseType<Envelope<PagedList<RootDepartmentDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status500InternalServerError)]
    public async Task<EndpointResult<PagedList<RootDepartmentDto>>> GetRootsAsync(
        [FromQuery] GetRootDepartmentsRequest request,
        [FromServices] IQueryHandler<GetRootDepartmentsQuery, PagedList<RootDepartmentDto>> handler,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received get root departments request. Page {Page}. Size {Size}. Prefetch {Prefetch}. TraceId {TraceId}",
            request.Page,
            request.Size,
            request.Prefetch,
            HttpContext.TraceIdentifier);

        var query = new GetRootDepartmentsQuery(
            request.Page,
            request.Size,
            request.Prefetch);

        return await handler.HandleAsync(query, cancellationToken);
    }

    [HttpGet("{parentId:guid}/children")]
    [ProducesResponseType<Envelope<PagedList<DepartmentNodeDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status500InternalServerError)]
    public async Task<EndpointResult<PagedList<DepartmentNodeDto>>> GetChildrenAsync(
        Guid parentId,
        [FromQuery] GetDepartmentChildrenRequest request,
        [FromServices] IQueryHandler<GetDepartmentChildrenQuery, PagedList<DepartmentNodeDto>> handler,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received get department children request. ParentId {ParentId}. Page {Page}. Size {Size}. TraceId {TraceId}",
            parentId,
            request.Page,
            request.Size,
            HttpContext.TraceIdentifier);

        var query = new GetDepartmentChildrenQuery(
            parentId,
            request.Page,
            request.Size);

        return await handler.HandleAsync(query, cancellationToken);
    }

    [HttpGet("top-positions")]
    [ProducesResponseType<Envelope<IReadOnlyList<TopDepartmentByPositionsDto>>>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status500InternalServerError)]
    public async Task<EndpointResult<IReadOnlyList<TopDepartmentByPositionsDto>>> GetTopByPositionsAsync(
        [FromServices]
        IQueryHandler<GetTopDepartmentsByPositionsQuery, IReadOnlyList<TopDepartmentByPositionsDto>> handler,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received get top departments by positions request. TraceId {TraceId}",
            HttpContext.TraceIdentifier);

        var query = new GetTopDepartmentsByPositionsQuery();

        return await handler.HandleAsync(query, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<Envelope<DepartmentDetailsDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status500InternalServerError)]
    public async Task<EndpointResult<DepartmentDetailsDto>> GetByIdAsync(
        Guid id,
        [FromServices] IQueryHandler<GetDepartmentByIdQuery, DepartmentDetailsDto> handler,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received get department by id request. DepartmentId {DepartmentId}. TraceId {TraceId}",
            id,
            HttpContext.TraceIdentifier);

        return await handler.HandleAsync(
            new GetDepartmentByIdQuery(id),
            cancellationToken);
    }

    [HttpPut("{departmentId:guid}/locations")]
    [ProducesResponseType<Envelope>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status500InternalServerError)]
    public async Task<EndpointResult> UpdateLocationsAsync(
        Guid departmentId,
        [FromBody] UpdateDepartmentLocationsRequest request,
        [FromServices] ICommandHandler<UpdateDepartmentLocationsCommand> handler,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received update department locations request. DepartmentId {DepartmentId}. LocationCount {LocationCount}. TraceId {TraceId}",
            departmentId,
            request.LocationIds.Length,
            HttpContext.TraceIdentifier);

        var command = new UpdateDepartmentLocationsCommand(
            departmentId,
            request.LocationIds);

        var result = await handler.Handle(command, cancellationToken);

        return result;
    }

    [HttpPut("{departmentId:guid}/parent")]
    [ProducesResponseType<Envelope>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status500InternalServerError)]
    public async Task<EndpointResult> UpdateParentAsync(
        Guid departmentId,
        [FromBody] UpdateDepartmentParentRequest request,
        [FromServices] ICommandHandler<UpdateDepartmentParentCommand> handler,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received update department parent request. DepartmentId {DepartmentId}. ParentId {ParentId}. TraceId {TraceId}",
            departmentId,
            request.ParentId,
            HttpContext.TraceIdentifier);

        var command = new UpdateDepartmentParentCommand(
            departmentId,
            request.ParentId);

        var result = await handler.Handle(command, cancellationToken);

        return result;
    }

    [HttpDelete("{departmentId:guid}")]
    [ProducesResponseType<Envelope>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status500InternalServerError)]
    public async Task<EndpointResult> SoftDeleteAsync(
        Guid departmentId,
        [FromServices] ICommandHandler<SoftDeleteDepartmentCommand> handler,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received department soft delete request. DepartmentId {DepartmentId}. TraceId {TraceId}",
            departmentId,
            HttpContext.TraceIdentifier);

        var command = new SoftDeleteDepartmentCommand(departmentId);

        return await handler.Handle(command, cancellationToken);
    }
}
