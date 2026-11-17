using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Cache;
using DirectoryService.Application.Abstractions.Database;
using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Application.Extensions.Validation;
using DirectoryService.Contracts.Departments;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.Shared;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Application.Features.Departments.Bulk;

public sealed class BulkDepartmentHandler(
    IValidator<BulkDepartmentCommand> validator,
    IReadDbContext readDbContext,
    IDepartmentBulkItemExecutor executor,
    ICacheInvalidator cacheInvalidator,
    ILogger<BulkDepartmentHandler> logger)
    : ICommandHandler<BulkDepartmentCommand, BulkDepartmentResultDto>
{
    public async Task<Result<BulkDepartmentResultDto, Error>> Handle(
        BulkDepartmentCommand command,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return validation.ToError();

        var ids = command.Ids!;
        var departmentIds = ids.Select(DepartmentId.Create).ToArray();
        var departments = await readDbContext.Departments.IgnoreQueryFilters().AsNoTracking()
            .Where(department => departmentIds.Contains(department.Id))
            .Select(department => new { department.Id, department.Depth })
            .ToListAsync(cancellationToken);
        var depths = departments.ToDictionary(department => department.Id.Value, department => (int)department.Depth);
        var parentsFirst = command.Action == DepartmentBulkAction.Restore ||
                           (command.Action == DepartmentBulkAction.Activity && command.IsActive == true);

        // Snapshot depth chooses a safe order; each operation rechecks current state under its own locks.
        var orderedIds = ids
            .OrderBy(id => depths.TryGetValue(id, out var depth)
                ? parentsFirst ? depth : -depth
                : int.MaxValue)
            .ThenBy(id => id)
            .ToArray();
        var succeededIds = new List<Guid>();
        var errors = new List<BulkDepartmentErrorDto>();

        try
        {
            foreach (var id in orderedIds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = await executor.ExecuteAsync(id, command.Action, command.IsActive, cancellationToken);
                if (result.IsSuccess)
                {
                    succeededIds.Add(id);
                    continue;
                }

                errors.Add(new BulkDepartmentErrorDto(
                    id,
                    result.Error.Messages.FirstOrDefault()?.Code ?? "department.bulk.failed",
                    string.Join("; ", result.Error.Messages.Select(message => message.Message))));
            }
        }
        finally
        {
            // Earlier items may already be committed when a later item is cancelled.
            if (succeededIds.Count > 0)
            {
                await cacheInvalidator.InvalidateAsync(
                    [CacheConstants.DEPARTMENTS_CACHE_TAG, CacheConstants.LOCATIONS_CACHE_TAG]);
            }
        }

        logger.LogInformation(
            "Bulk department {Action} completed. Succeeded {SucceededCount}. Failed {FailedCount}",
            command.Action, succeededIds.Count, errors.Count);

        return new BulkDepartmentResultDto(succeededIds, errors);
    }
}
