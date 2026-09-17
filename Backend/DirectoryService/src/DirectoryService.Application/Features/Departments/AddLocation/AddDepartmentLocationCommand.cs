using DirectoryService.Application.Abstractions.Handlers;

namespace DirectoryService.Application.Features.Departments.AddLocation;

public sealed record AddDepartmentLocationCommand(
    Guid DepartmentId,
    Guid LocationId) : ICommand;
