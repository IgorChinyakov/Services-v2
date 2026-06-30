using DirectoryService.Application.Abstractions.Handlers;

namespace DirectoryService.Application.Features.Departments.Create;

public sealed record CreateDepartmentCommand(
    string Name,
    string Identifier,
    Guid? ParentId,
    IReadOnlyCollection<Guid> LocationIds) : ICommand;
