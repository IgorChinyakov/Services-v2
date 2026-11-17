using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Application.Features.Departments.Bulk;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace DirectoryService.Infrastructure.Repositories;

public sealed class DepartmentBulkItemExecutor(IServiceScopeFactory scopeFactory) : IDepartmentBulkItemExecutor
{
    public async Task<UnitResult<Error>> ExecuteAsync(
        Guid id,
        DepartmentBulkAction action,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        // A failed item's tracked entities must never leak into the next item's transaction.
        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDepartmentRepository>();
        var departmentId = DepartmentId.Create(id);

        switch (action)
        {
            case DepartmentBulkAction.Archive:
            {
                var result = await repository.SoftDeleteAsync(departmentId, cancellationToken);
                return result.IsSuccess ? UnitResult.Success<Error>() : result.Error;
            }

            case DepartmentBulkAction.Restore:
            {
                var result = await repository.RestoreAsync(departmentId, cancellationToken);
                return result.IsSuccess ? UnitResult.Success<Error>() : result.Error;
            }

            case DepartmentBulkAction.Activity:
                return await repository.UpdateActivityAsync(departmentId, isActive!.Value, cancellationToken);
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }
    }
}
