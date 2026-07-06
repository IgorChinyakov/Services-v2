using DirectoryService.Application.Abstractions.Handlers;

namespace DirectoryService.Application.Features.Departments.UpdateParent;

public sealed record UpdateDepartmentParentCommand(
    Guid DepartmentId,
    Guid? ParentId) : ICommand;
