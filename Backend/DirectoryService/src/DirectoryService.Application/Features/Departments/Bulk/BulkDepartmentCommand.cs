using DirectoryService.Application.Abstractions.Handlers;

namespace DirectoryService.Application.Features.Departments.Bulk;

public enum DepartmentBulkAction
{
    Archive,
    Restore,
    Activity,
}

public sealed record BulkDepartmentCommand(
    Guid[]? Ids,
    DepartmentBulkAction Action,
    bool? IsActive = null) : ICommand;
