using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Features.Departments.AddLocation;
using DirectoryService.Application.Features.Departments.AddPosition;
using DirectoryService.Application.Features.Departments.Create;
using DirectoryService.Application.Features.Departments.Get;
using DirectoryService.Application.Features.Departments.GetAncestors;
using DirectoryService.Application.Features.Departments.GetById;
using DirectoryService.Application.Features.Departments.GetChildren;
using DirectoryService.Application.Features.Departments.GetTopByPositions;
using DirectoryService.Application.Features.Departments.GetTree;
using DirectoryService.Application.Features.Departments.RemoveLocation;
using DirectoryService.Application.Features.Departments.RemovePosition;
using DirectoryService.Application.Features.Departments.SearchTree;
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

    [HttpGet("tree")]
    [ProducesResponseType<Envelope<IReadOnlyList<DepartmentTreeNodeDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status500InternalServerError)]
    public async Task<EndpointResult<IReadOnlyList<DepartmentTreeNodeDto>>> GetTreeAsync(
        [FromServices]
        IQueryHandler<GetDepartmentTreeQuery, IReadOnlyList<DepartmentTreeNodeDto>> handler,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received get department tree request. TraceId {TraceId}",
            HttpContext.TraceIdentifier);

        return await handler.HandleAsync(new GetDepartmentTreeQuery(), cancellationToken);
    }

    [HttpGet("{departmentId:guid}/children")]
    [ProducesResponseType<Envelope<IReadOnlyList<DepartmentTreeNodeDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status500InternalServerError)]
    public async Task<EndpointResult<IReadOnlyList<DepartmentTreeNodeDto>>> GetChildrenAsync(
        Guid departmentId,
        [FromServices]
        IQueryHandler<GetDepartmentChildrenQuery, IReadOnlyList<DepartmentTreeNodeDto>> handler,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received get department children request. DepartmentId {DepartmentId}. TraceId {TraceId}",
            departmentId,
            HttpContext.TraceIdentifier);

        return await handler.HandleAsync(
            new GetDepartmentChildrenQuery(departmentId),
            cancellationToken);
    }

    [HttpGet("{departmentId:guid}/ancestors")]
    [ProducesResponseType<Envelope<IReadOnlyList<DepartmentTreeNodeDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status500InternalServerError)]
    public async Task<EndpointResult<IReadOnlyList<DepartmentTreeNodeDto>>> GetAncestorsAsync(
        Guid departmentId,
        [FromServices]
        IQueryHandler<GetDepartmentAncestorsQuery, IReadOnlyList<DepartmentTreeNodeDto>> handler,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received get department ancestors request. DepartmentId {DepartmentId}. TraceId {TraceId}",
            departmentId,
            HttpContext.TraceIdentifier);

        return await handler.HandleAsync(
            new GetDepartmentAncestorsQuery(departmentId),
            cancellationToken);
    }

    [HttpGet("tree/search")]
    [ProducesResponseType<Envelope<IReadOnlyList<DepartmentTreeSearchResultDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status500InternalServerError)]
    public async Task<EndpointResult<IReadOnlyList<DepartmentTreeSearchResultDto>>> SearchTreeAsync(
        [FromQuery(Name = "q")] string search,
        [FromServices]
        IQueryHandler<SearchDepartmentTreeQuery, IReadOnlyList<DepartmentTreeSearchResultDto>> handler,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received department tree search request. Search {Search}. TraceId {TraceId}",
            search,
            HttpContext.TraceIdentifier);

        return await handler.HandleAsync(
            new SearchDepartmentTreeQuery(search),
            cancellationToken);
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
    [ProducesResponseType<Envelope<DepartmentMoveResultDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status500InternalServerError)]
    public async Task<EndpointResult<DepartmentMoveResultDto>> UpdateParentAsync(
        Guid departmentId,
        [FromBody] UpdateDepartmentParentRequest request,
        [FromServices]
        ICommandHandler<UpdateDepartmentParentCommand, DepartmentMoveResultDto> handler,
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
