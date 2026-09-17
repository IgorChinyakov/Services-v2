using DirectoryService.Application.Abstractions.Handlers;

namespace DirectoryService.Application.Features.Departments.SearchTree;

public sealed record SearchDepartmentTreeQuery(string Search) : IQuery;
