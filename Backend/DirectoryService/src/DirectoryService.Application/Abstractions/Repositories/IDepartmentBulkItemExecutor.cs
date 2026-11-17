using CSharpFunctionalExtensions;
using DirectoryService.Application.Features.Departments.Bulk;
using DirectoryService.Domain.Shared;

namespace DirectoryService.Application.Abstractions.Repositories;

public interface IDepartmentBulkItemExecutor
{
    Task<UnitResult<Error>> ExecuteAsync(
        Guid id,
        DepartmentBulkAction action,
        bool? isActive,
        CancellationToken cancellationToken);
}
