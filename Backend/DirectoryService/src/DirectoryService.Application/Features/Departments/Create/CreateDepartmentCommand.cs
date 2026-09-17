using DirectoryService.Application.Abstractions.Handlers;

namespace DirectoryService.Application.Features.Departments.Create;

public sealed record CreateDepartmentCommand(
    string Name,
    string Slug,
    Guid? ParentId,
    IReadOnlyCollection<Guid> LocationIds) : ICommand;
