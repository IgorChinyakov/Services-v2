using DirectoryService.Application.Abstractions.Handlers;

namespace DirectoryService.Application.Features.Departments.UpdateLocations;

public sealed record UpdateDepartmentLocationsCommand(
    Guid DepartmentId,
    IReadOnlyCollection<Guid> LocationIds) : ICommand;
