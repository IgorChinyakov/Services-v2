using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Features.Departments.Create;
using DirectoryService.Application.Features.Departments.UpdateLocations;
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
    private readonly ILogger<DepartmentsController> _logger;

    public DepartmentsController(
        ICommandHandler<CreateDepartmentCommand, Guid> createDepartmentHandler,
        ICommandHandler<UpdateDepartmentLocationsCommand> updateDepartmentLocationsHandler,
        ILogger<DepartmentsController> logger)
    {
        _createDepartmentHandler = createDepartmentHandler;
        _updateDepartmentLocationsHandler = updateDepartmentLocationsHandler;
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
}
