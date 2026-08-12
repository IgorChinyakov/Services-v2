using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Features.Departments.Create;
using DirectoryService.Application.Features.Departments.GetChildren;
using DirectoryService.Application.Features.Departments.GetRoots;
using DirectoryService.Application.Features.Departments.GetTopByPositions;
using DirectoryService.Application.Features.Departments.SoftDelete;
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
    private readonly ICommandHandler<CreateDepartmentCommand, Guid> _createDepartmentHandler;
    private readonly ICommandHandler<UpdateDepartmentLocationsCommand> _updateDepartmentLocationsHandler;
    private readonly ICommandHandler<UpdateDepartmentParentCommand> _updateDepartmentParentHandler;
    private readonly ICommandHandler<SoftDeleteDepartmentCommand> _softDeleteDepartmentHandler;
    private readonly IQueryHandler<
        GetRootDepartmentsQuery,
        PagedList<RootDepartmentDto>> _getRootDepartmentsHandler;

    private readonly IQueryHandler<
        GetDepartmentChildrenQuery,
        PagedList<DepartmentNodeDto>> _getDepartmentChildrenHandler;

    private readonly IQueryHandler<
        GetTopDepartmentsByPositionsQuery,
        IReadOnlyList<TopDepartmentByPositionsDto>> _getTopDepartmentsByPositionsHandler;

    private readonly ILogger<DepartmentsController> _logger;

    public DepartmentsController(
        ICommandHandler<CreateDepartmentCommand, Guid> createDepartmentHandler,
        ICommandHandler<UpdateDepartmentLocationsCommand> updateDepartmentLocationsHandler,
        ICommandHandler<UpdateDepartmentParentCommand> updateDepartmentParentHandler,
        ICommandHandler<SoftDeleteDepartmentCommand> softDeleteDepartmentHandler,
        IQueryHandler<
            GetRootDepartmentsQuery,
            PagedList<RootDepartmentDto>> getRootDepartmentsHandler,
        IQueryHandler<
            GetDepartmentChildrenQuery,
            PagedList<DepartmentNodeDto>> getDepartmentChildrenHandler,
        IQueryHandler<
            GetTopDepartmentsByPositionsQuery,
            IReadOnlyList<TopDepartmentByPositionsDto>> getTopDepartmentsByPositionsHandler,
        ILogger<DepartmentsController> logger)
    {
        _createDepartmentHandler = createDepartmentHandler;
        _updateDepartmentLocationsHandler = updateDepartmentLocationsHandler;
        _updateDepartmentParentHandler = updateDepartmentParentHandler;
        _softDeleteDepartmentHandler = softDeleteDepartmentHandler;
        _getRootDepartmentsHandler = getRootDepartmentsHandler;
        _getDepartmentChildrenHandler = getDepartmentChildrenHandler;
        _getTopDepartmentsByPositionsHandler = getTopDepartmentsByPositionsHandler;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType<EndpointResult<Guid>>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(EndpointResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(EndpointResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(EndpointResult), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(EndpointResult), StatusCodes.Status500InternalServerError)]
    public async Task<EndpointResult<Guid>> CreateAsync(
        [FromBody] CreateDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received create department request. Identifier {DepartmentIdentifier}. ParentId {ParentId}. TraceId {TraceId}",
            request.Identifier,
            request.ParentId,
            HttpContext.TraceIdentifier);

        var command = new CreateDepartmentCommand(
            request.Name,
            request.Identifier,
            request.ParentId,
            request.LocationIds);

        var result = await _createDepartmentHandler.Handle(command, cancellationToken);

        return result;
    }

    [HttpGet("roots")]
    [ProducesResponseType<EndpointResult<PagedList<RootDepartmentDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(EndpointResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(EndpointResult), StatusCodes.Status500InternalServerError)]
    public async Task<EndpointResult<PagedList<RootDepartmentDto>>> GetRootsAsync(
        [FromQuery] GetRootDepartmentsRequest request,
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

        return await _getRootDepartmentsHandler.HandleAsync(query, cancellationToken);
    }

    [HttpGet("{parentId:guid}/children")]
    [ProducesResponseType<EndpointResult<PagedList<DepartmentNodeDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(EndpointResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(EndpointResult), StatusCodes.Status500InternalServerError)]
    public async Task<EndpointResult<PagedList<DepartmentNodeDto>>> GetChildrenAsync(
        Guid parentId,
        [FromQuery] GetDepartmentChildrenRequest request,
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

        return await _getDepartmentChildrenHandler.HandleAsync(query, cancellationToken);
    }

    [HttpGet("top-positions")]
    [ProducesResponseType<EndpointResult<IReadOnlyList<TopDepartmentByPositionsDto>>>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(EndpointResult), StatusCodes.Status500InternalServerError)]
    public async Task<EndpointResult<IReadOnlyList<TopDepartmentByPositionsDto>>> GetTopByPositionsAsync(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received get top departments by positions request. TraceId {TraceId}",
            HttpContext.TraceIdentifier);

        var query = new GetTopDepartmentsByPositionsQuery();

        return await _getTopDepartmentsByPositionsHandler.HandleAsync(query, cancellationToken);
    }

    [HttpPut("{departmentId:guid}/locations")]
    [ProducesResponseType<EndpointResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(EndpointResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(EndpointResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(EndpointResult), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(EndpointResult), StatusCodes.Status500InternalServerError)]
    public async Task<EndpointResult> UpdateLocationsAsync(
        Guid departmentId,
        [FromBody] UpdateDepartmentLocationsRequest request,
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

        var result = await _updateDepartmentLocationsHandler.Handle(command, cancellationToken);

        return result;
    }

    [HttpPut("{departmentId:guid}/parent")]
    [ProducesResponseType<EndpointResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(EndpointResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(EndpointResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(EndpointResult), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(EndpointResult), StatusCodes.Status500InternalServerError)]
    public async Task<EndpointResult> UpdateParentAsync(
        Guid departmentId,
        [FromBody] UpdateDepartmentParentRequest request,
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

        var result = await _updateDepartmentParentHandler.Handle(command, cancellationToken);

        return result;
    }

    [HttpDelete("{departmentId:guid}")]
    [ProducesResponseType<EndpointResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(EndpointResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(EndpointResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(EndpointResult), StatusCodes.Status500InternalServerError)]
    public async Task<EndpointResult> SoftDeleteAsync(
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received department soft delete request. DepartmentId {DepartmentId}. TraceId {TraceId}",
            departmentId,
            HttpContext.TraceIdentifier);

        var command = new SoftDeleteDepartmentCommand(departmentId);

        return await _softDeleteDepartmentHandler.Handle(command, cancellationToken);
    }
}
