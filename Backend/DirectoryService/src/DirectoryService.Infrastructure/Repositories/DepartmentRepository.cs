using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Database;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Domain.Entities;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.Errors;
using DirectoryService.Domain.Shared;
using DirectoryService.Infrastructure.Database;
using DirectoryService.Infrastructure.Errors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace DirectoryService.Infrastructure.Repositories;

public sealed class DepartmentRepository : IDepartmentRepository
{
    private readonly DirectoryServiceDbContext _dbContext;
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<DepartmentRepository> _logger;

    public DepartmentRepository(
        DirectoryServiceDbContext dbContext,
        ILogger<DepartmentRepository> logger,
        ITransactionManager transactionManager)
    {
        _dbContext = dbContext;
        _logger = logger;
        _transactionManager = transactionManager;
    }

    public async Task<Result<bool, Error>> SlugExistsAsync(
        string slug,
        CancellationToken cancellationToken)
    {
        try
        {
            var normalizedSlug = slug.Trim();

            return await _dbContext.Departments
                .AsNoTracking()
                .AnyAsync(
                    department => department.Slug.Value == normalizedSlug,
                    cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to check department slug uniqueness. DepartmentSlug {DepartmentSlug}",
                slug);

            return DatabaseErrors.OperationFailed("check department slug uniqueness");
        }
    }

    public async Task<Result<Department, Error>> GetActiveByIdAsync(
        DepartmentId departmentId,
        CancellationToken cancellationToken)
    {
        try
        {
            var department = await _dbContext.Departments
                .FirstOrDefaultAsync(
                    item => item.Id == departmentId && item.IsActive,
                    cancellationToken);

            if (department is null)
                return DepartmentErrors.NotFound(departmentId.Value, nameof(departmentId));

            return department;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to load active department. DepartmentId {DepartmentId}",
                departmentId.Value);

            return DatabaseErrors.OperationFailed("load active department");
        }
    }

    public async Task<Result<IReadOnlyCollection<LocationId>, Error>> GetMissingActiveLocationIdsAsync(
        IReadOnlyCollection<LocationId> locationIds,
        CancellationToken cancellationToken)
    {
        try
        {
            var requestedLocationIds = locationIds.Distinct().ToArray();
            var missingLocationIds = new List<LocationId>();

            foreach (var locationId in requestedLocationIds)
            {
                var exists = await _dbContext.Locations
                    .AsNoTracking()
                    .AnyAsync(
                        location => location.IsActive && location.Id == locationId,
                        cancellationToken);

                if (!exists)
                    missingLocationIds.Add(locationId);
            }

            return Result.Success<IReadOnlyCollection<LocationId>, Error>(missingLocationIds);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to check active locations. LocationIds {@LocationIds}",
                locationIds.Select(id => id.Value));

            return DatabaseErrors.OperationFailed("check active locations");
        }
    }

    public async Task<Result<int, Error>> ReplaceLocationsAsync(
        DepartmentId departmentId,
        IReadOnlyCollection<LocationId> locationIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(departmentId);
        ArgumentNullException.ThrowIfNull(locationIds);

        try
        {
            var distinctLocationIds = locationIds.Distinct().ToArray();

            _logger.LogDebug(
                "Replacing department locations. DepartmentId {DepartmentId}. LocationCount {LocationCount}",
                departmentId.Value,
                distinctLocationIds.Length);

            await _dbContext.Set<DepartmentLocation>()
                .IgnoreQueryFilters()
                .Where(departmentLocation => departmentLocation.DepartmentId == departmentId)
                .ExecuteDeleteAsync(cancellationToken);

            var departmentLocations = distinctLocationIds
                .Select(locationId => new DepartmentLocation(departmentId, locationId))
                .ToArray();

            await _dbContext.Set<DepartmentLocation>().AddRangeAsync(departmentLocations, cancellationToken);

            _logger.LogDebug(
                "Department locations replacement added to change tracker. DepartmentId {DepartmentId}. LocationCount {LocationCount}",
                departmentId.Value,
                departmentLocations.Length);

            return departmentLocations.Length;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to replace department locations. DepartmentId {DepartmentId}. LocationIds {@LocationIds}",
                departmentId.Value,
                locationIds.Select(id => id.Value));

            return DatabaseErrors.OperationFailed("replace department locations");
        }
    }

    public async Task<Result<bool, Error>> LocationLinkExistsAsync(
        DepartmentId departmentId,
        LocationId locationId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.Set<DepartmentLocation>()
                .AsNoTracking()
                .AnyAsync(
                    link => link.DepartmentId == departmentId && link.LocationId == locationId,
                    cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to check department location link. DepartmentId {DepartmentId}. LocationId {LocationId}",
                departmentId.Value,
                locationId.Value);

            return DatabaseErrors.OperationFailed("check department location link");
        }
    }

    public async Task<Result<DepartmentLocation, Error>> AddLocationLinkAsync(
        DepartmentId departmentId,
        LocationId locationId,
        CancellationToken cancellationToken)
    {
        var link = new DepartmentLocation(departmentId, locationId);
        await _dbContext.Set<DepartmentLocation>().AddAsync(link, cancellationToken);

        return link;
    }

    public async Task<Result<bool, Error>> RemoveLocationLinkAsync(
        DepartmentId departmentId,
        LocationId locationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var link = await _dbContext.Set<DepartmentLocation>()
                .FirstOrDefaultAsync(
                    item => item.DepartmentId == departmentId && item.LocationId == locationId,
                    cancellationToken);

            if (link is null)
                return false;

            _dbContext.Set<DepartmentLocation>().Remove(link);
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to remove department location link. DepartmentId {DepartmentId}. LocationId {LocationId}",
                departmentId.Value,
                locationId.Value);

            return DatabaseErrors.OperationFailed("remove department location link");
        }
    }

    public async Task<Result<bool, Error>> PositionLinkExistsAsync(
        DepartmentId departmentId,
        PositionId positionId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.Set<DepartmentPosition>()
                .AsNoTracking()
                .AnyAsync(
                    link => link.DepartmentId == departmentId && link.PositionId == positionId,
                    cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to check department position link. DepartmentId {DepartmentId}. PositionId {PositionId}",
                departmentId.Value,
                positionId.Value);

            return DatabaseErrors.OperationFailed("check department position link");
        }
    }

    public async Task<Result<DepartmentPosition, Error>> AddPositionLinkAsync(
        DepartmentId departmentId,
        PositionId positionId,
        CancellationToken cancellationToken)
    {
        var link = new DepartmentPosition(departmentId, positionId);
        await _dbContext.Set<DepartmentPosition>().AddAsync(link, cancellationToken);

        return link;
    }

    public async Task<Result<bool, Error>> RemovePositionLinkAsync(
        DepartmentId departmentId,
        PositionId positionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var link = await _dbContext.Set<DepartmentPosition>()
                .FirstOrDefaultAsync(
                    item => item.DepartmentId == departmentId && item.PositionId == positionId,
                    cancellationToken);

            if (link is null)
                return false;

            _dbContext.Set<DepartmentPosition>().Remove(link);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to remove department position link. DepartmentId {DepartmentId}. PositionId {PositionId}",
                departmentId.Value,
                positionId.Value);

            return DatabaseErrors.OperationFailed("remove department position link");
        }
    }

    public async Task<Result<DepartmentMoveResult, Error>> MoveParentAsync(
        DepartmentId departmentId,
        DepartmentId? parentId,
        CancellationToken cancellationToken)
    {
        try
        {
            Guid[] departmentIdsToLock = parentId is null || parentId == departmentId
                ? [departmentId.Value]
                : [departmentId.Value, parentId.Value];

            var lockedDepartments = await _dbContext.Database
                .SqlQuery<DepartmentMoveInfo>($"""
                                               SELECT
                                                   id AS "Id",
                                                   parent_id AS "ParentId",
                                                   slug AS "Slug",
                                                   path::text AS "Path",
                                                   depth AS "Depth",
                                                   is_active AS "IsActive",
                                                   updated_at AS "UpdatedAt",
                                                   deleted_at AS "DeletedAt"
                                               FROM departments
                                               WHERE id = ANY({departmentIdsToLock})
                                               ORDER BY id
                                               FOR UPDATE
                                               """)
                .ToListAsync(cancellationToken);

            var department = lockedDepartments.SingleOrDefault(item => item.Id == departmentId.Value);

            if (department is null || !department.IsActive || department.DeletedAt is not null)
                return DepartmentErrors.NotFound(departmentId.Value, nameof(departmentId));

            if (parentId == departmentId)
            {
                return DepartmentErrors.MoveParentIsSelf(
                    nameof(parentId));
            }

            DepartmentMoveInfo? parent = null;
            if (parentId is not null)
            {
                parent = lockedDepartments.SingleOrDefault(item => item.Id == parentId.Value);

                if (parent is null)
                    return DepartmentErrors.ParentNotFound(parentId.Value, nameof(parentId));

                if (!parent.IsActive || parent.DeletedAt is not null)
                    return DepartmentErrors.MoveParentDeleted(nameof(parentId));
            }

            var requestedParentId = parentId?.Value;
            if (department.ParentId == requestedParentId)
            {
                return new DepartmentMoveResult(
                    department.Id,
                    department.ParentId,
                    department.Path,
                    department.Depth,
                    department.UpdatedAt,
                    false);
            }

            bool isParentInsideDepartmentSubtree = false;
            if (parent is not null)
            {
                isParentInsideDepartmentSubtree = await _dbContext.Database.SqlQuery<bool>(
                    $"""
                        SELECT {parent.Path}::ltree <@ {department.Path}::ltree AS "Value"
                     """).SingleAsync(cancellationToken);
            }

            if (isParentInsideDepartmentSubtree)
                return DepartmentErrors.MoveCycle(nameof(parentId));

            string oldPath = department.Path;
            string newPath = parent is null ? department.Slug : parent.Path + "." + department.Slug;
            Guid? newParentId = parent is null ? null : parent.Id;
            short newDepth = parent is null
                ? (short)0
                : checked((short)(parent.Depth + 1));
            int depthDelta = newDepth - department.Depth;
            var updatedAt = DateTime.UtcNow;

            var updatedRows = await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 UPDATE departments
                 SET
                     parent_id = CASE 
                        WHEN id = {departmentId.Value} 
                        THEN {newParentId} 
                        ELSE parent_id 
                    END,
                    path = CASE
                        WHEN id = {departmentId.Value} THEN {newPath}::ltree
                        ELSE {newPath}::ltree || subpath(path, nlevel({oldPath}::ltree))
                    END,
                    updated_at = {updatedAt},
                    depth = (depth + {depthDelta})::smallint
                 WHERE path <@ {oldPath}::ltree
                 """, cancellationToken);

            _logger.LogInformation(
                "Department parent was updated. DepartmentId {DepartmentId}. ParentId {ParentId}. UpdatedRows {UpdatedRows}",
                departmentId.Value,
                parentId?.Value,
                updatedRows);

            return new DepartmentMoveResult(
                department.Id,
                newParentId,
                newPath,
                newDepth,
                updatedAt,
                true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (PostgresException exception) when (IsMoveConflict(exception))
        {
            _logger.LogWarning(
                exception,
                "Concurrent department move conflict. DepartmentId {DepartmentId}. ParentId {ParentId}. SqlState {SqlState}",
                departmentId.Value,
                parentId?.Value,
                exception.SqlState);

            return DepartmentErrors.MoveConflict();
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to move department parent. DepartmentId {DepartmentId}. ParentId {ParentId}",
                departmentId.Value,
                parentId?.Value);

            return DatabaseErrors.OperationFailed("move department parent");
        }
    }

    public async Task<Result<DateTime, Error>> SoftDeleteAsync(
        DepartmentId departmentId,
        CancellationToken cancellationToken)
    {
        var transactionScopeResult = await _transactionManager.BeginTransactionAsync(cancellationToken);
        if (transactionScopeResult.IsFailure)
            return transactionScopeResult.Error;

        await using var transactionScope = transactionScopeResult.Value;

        try
        {
            // добавил order by по id для при блокировке для избежания deadlock, актуально и для блокировок, которые ниже
            var lockedDepartmentId = await _dbContext.Database.SqlQuery<Guid>(
                $"""
                    select d.id as "Value"
                    from departments d
                    where d.id = {departmentId.Value} and
                          d.deleted_at is null
                    for update
                 """).SingleOrDefaultAsync(cancellationToken);

            if (lockedDepartmentId == Guid.Empty)
            {
                var transactionRollbackResult = await transactionScope.RollbackAsync(cancellationToken);
                if (transactionRollbackResult.IsFailure)
                    return transactionRollbackResult.Error;

                return DepartmentErrors.NotFound(departmentId.Value, nameof(departmentId));
            }

            var lockedDepartment = await _dbContext.Departments.IgnoreQueryFilters()
                .FirstOrDefaultAsync(d => d.Id == departmentId && d.DeletedAt == null, cancellationToken);

            if (lockedDepartment is null)
            {
                var transactionRollbackResult = await transactionScope.RollbackAsync(cancellationToken);
                if (transactionRollbackResult.IsFailure)
                    return transactionRollbackResult.Error;

                return DepartmentErrors.NotFound(departmentId.Value, nameof(departmentId));
            }

            var oldPath = lockedDepartment.Path;

            lockedDepartment.SoftDelete();

            lockedDepartment.MarkAsDeleted();

            var newPath = lockedDepartment.Path;

            _ = await _dbContext.Database.SqlQuery<Guid>(
                $"""
                     select
                         d.id AS "Value"
                     from departments d
                     where d.path <@ {oldPath}::ltree
                     order by d.id
                     for update
                 """).ToListAsync(cancellationToken);

            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 UPDATE departments
                 SET path =
                         {newPath}::ltree ||
                         subpath(path, nlevel({oldPath}::ltree)),
                     updated_at = NOW()
                 WHERE path <@ {oldPath}::ltree
                   AND id <> {departmentId.Value}
                 """,
                cancellationToken);

            _ = await _dbContext.Database.SqlQuery<Guid>(
                $"""
                     select
                         l.id AS "Value"
                     from locations l
                     join department_locations dl on dl.location_id = l.id
                     where dl.department_id = {departmentId.Value}
                     order by l.id
                     for update of l
                 """).ToListAsync(cancellationToken);

            _ = await _dbContext.Database.SqlQuery<Guid>(
                $"""
                     select
                         p.id AS "Value"
                     from positions p
                     join department_positions dp on dp.position_id = p.id
                     where dp.department_id = {departmentId.Value}
                     order by p.id
                     for update of p
                 """).ToListAsync(cancellationToken);

            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 UPDATE locations
                 SET is_active = FALSE,
                     deleted_at = NOW(),
                     updated_at = NOW()
                 WHERE id IN (
                     SELECT dl.location_id
                     FROM department_locations dl
                     WHERE dl.department_id = {departmentId.Value}
                       AND NOT EXISTS (
                           SELECT 1
                           FROM department_locations other_dl
                           JOIN departments other_d
                             ON other_d.id = other_dl.department_id
                           WHERE other_dl.location_id = dl.location_id
                             AND other_dl.department_id <> {departmentId.Value}
                             AND other_d.is_active = TRUE
                       )
                 )
                   AND is_active = TRUE
                 """,
                cancellationToken);

            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 UPDATE positions
                 SET is_active = FALSE,
                     deleted_at = NOW(),
                     updated_at = NOW()
                 WHERE id IN (
                     SELECT dp.position_id
                     FROM department_positions dp
                     WHERE dp.department_id = {departmentId.Value}
                       AND NOT EXISTS (
                           SELECT 1
                           FROM department_positions other_dp
                           JOIN departments other_d
                             ON other_d.id = other_dp.department_id
                           WHERE other_dp.position_id = dp.position_id
                             AND other_dp.department_id <> {departmentId.Value}
                             AND other_d.is_active = TRUE
                       )
                 )
                   AND is_active = TRUE
                 """,
                cancellationToken);

            var saveChangesResult = await _transactionManager.SaveChangesAsync(
                nameof(Department),
                cancellationToken: cancellationToken);
            if (saveChangesResult.IsFailure)
            {
                var transactionRollbackResult = await transactionScope.RollbackAsync(cancellationToken);
                if (transactionRollbackResult.IsFailure)
                    return transactionRollbackResult.Error;

                return saveChangesResult.Error;
            }

            var commitResult = await transactionScope.CommitAsync(cancellationToken);
            if (commitResult.IsFailure)
                return commitResult.Error;

            return lockedDepartment.DeletedAt!.Value;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "Failed to soft delete department. DepartmentId {DepartmentId}",
                departmentId.Value);

            var rollbackResult = await transactionScope.RollbackAsync(cancellationToken);
            if (rollbackResult.IsFailure)
                return rollbackResult.Error;

            return DatabaseErrors.OperationFailed("soft delete department");
        }
    }

    public async Task<UnitResult<Error>> UpdateActivityAsync(
        DepartmentId departmentId,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var transactionResult = await _transactionManager.BeginTransactionAsync(cancellationToken);
        if (transactionResult.IsFailure)
            return transactionResult.Error;

        await using var transaction = transactionResult.Value;
        try
        {
            // Serialize activity checks and updates while allowing concurrent reads.
            await _dbContext.Database.ExecuteSqlRawAsync(
                "LOCK TABLE departments IN SHARE ROW EXCLUSIVE MODE", cancellationToken);

            var department = await _dbContext.Departments.IgnoreQueryFilters().AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == departmentId, cancellationToken);
            if (department is null)
                return DepartmentErrors.NotFound(departmentId.Value);
            if (department.DeletedAt is not null)
                return DepartmentErrors.ActivityArchived();

            if (department.IsActive != isActive)
            {
                var blocked = isActive
                    ? await _dbContext.Database.SqlQuery<bool>($"""
                        SELECT EXISTS (
                            SELECT 1 FROM departments
                            WHERE path @> {department.Path}::ltree AND id <> {departmentId.Value}
                              AND (is_active = FALSE OR deleted_at IS NOT NULL)
                        ) AS "Value"
                        """).SingleAsync(cancellationToken)
                    : await _dbContext.Database.SqlQuery<bool>($"""
                        SELECT EXISTS (
                            SELECT 1 FROM departments
                            WHERE path <@ {department.Path}::ltree AND id <> {departmentId.Value}
                              AND is_active = TRUE AND deleted_at IS NULL
                        ) AS "Value"
                        """).SingleAsync(cancellationToken);
                if (blocked)
                {
                    return isActive
                        ? DepartmentErrors.ActivityParentInactive()
                        : DepartmentErrors.ActivityHasActiveDescendants();
                }

                await _dbContext.Departments.IgnoreQueryFilters()
                    .Where(item => item.Id == departmentId)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(item => item.IsActive, isActive)
                            .SetProperty(item => item.UpdatedAt, DateTime.UtcNow),
                        cancellationToken);
            }

            return await transaction.CommitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to change department activity. DepartmentId {DepartmentId}",
                departmentId.Value);
            return DatabaseErrors.OperationFailed("change department activity");
        }
    }

    public async Task<Result<DateTime, Error>> RestoreAsync(
        DepartmentId departmentId,
        CancellationToken cancellationToken)
    {
        var transactionResult = await _transactionManager.BeginTransactionAsync(cancellationToken);
        if (transactionResult.IsFailure)
            return transactionResult.Error;

        await using var transaction = transactionResult.Value;

        try
        {
            var snapshot = await _dbContext.Departments.IgnoreQueryFilters().AsNoTracking()
                .Where(department => department.Id == departmentId)
                .Select(department => new { department.ParentId })
                .SingleOrDefaultAsync(cancellationToken);
            if (snapshot is null)
                return DepartmentErrors.ArchivedNotFound(departmentId.Value);

            Guid[] idsToLock = snapshot.ParentId is null
                ? [departmentId.Value]
                : [departmentId.Value, snapshot.ParentId.Value];

            var lockedDepartments = await _dbContext.Database.SqlQuery<DepartmentMoveInfo>(
                $"""
                 SELECT id AS "Id", parent_id AS "ParentId", slug AS "Slug",
                        path::text AS "Path", depth AS "Depth", is_active AS "IsActive",
                        updated_at AS "UpdatedAt", deleted_at AS "DeletedAt"
                 FROM departments
                 WHERE id = ANY({idsToLock})
                 ORDER BY id
                 FOR UPDATE
                 """).ToListAsync(cancellationToken);

            var department = lockedDepartments.SingleOrDefault(item => item.Id == departmentId.Value);
            if (department is null || department.IsActive || department.DeletedAt is null)
                return DepartmentErrors.ArchivedNotFound(departmentId.Value);
            if (department.ParentId != snapshot.ParentId?.Value)
                return DepartmentErrors.RestoreConflict();

            var parent = department.ParentId is { } parentId
                ? lockedDepartments.SingleOrDefault(item => item.Id == parentId)
                : null;
            if (department.ParentId is not null &&
                (parent is null || !parent.IsActive || parent.DeletedAt is not null))
                return DepartmentErrors.RestoreParentDeleted();

            var oldPath = department.Path;
            var newPath = parent is null ? department.Slug : $"{parent.Path}.{department.Slug}";
            var depthDelta = (parent is null ? 0 : parent.Depth + 1) - department.Depth;
            var updatedAt = DateTime.UtcNow;

            // Lock archived descendants too: restoring a parent also rewrites their path prefix.
            _ = await _dbContext.Database.SqlQuery<Guid>(
                $"""
                 SELECT id AS "Value" FROM departments
                 WHERE path <@ {oldPath}::ltree
                 ORDER BY id
                 FOR UPDATE
                 """).ToListAsync(cancellationToken);

            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 UPDATE departments
                 SET path = CASE
                         WHEN id = {departmentId.Value} THEN {newPath}::ltree
                         ELSE {newPath}::ltree || subpath(path, nlevel({oldPath}::ltree))
                     END,
                     depth = (depth + {depthDelta})::smallint,
                     is_active = CASE WHEN id = {departmentId.Value} THEN TRUE ELSE is_active END,
                     deleted_at = CASE WHEN id = {departmentId.Value} THEN NULL ELSE deleted_at END,
                     updated_at = {updatedAt}
                 WHERE path <@ {oldPath}::ltree
                 """, cancellationToken);

            var commitResult = await transaction.CommitAsync(cancellationToken);
            if (commitResult.IsFailure)
                return commitResult.Error;

            return updatedAt;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            var rollbackResult = await transaction.RollbackAsync(cancellationToken);
            if (rollbackResult.IsFailure)
                return rollbackResult.Error;

            if (exception is PostgresException postgresException)
            {
                if (postgresException.SqlState == PostgresErrorCodes.UniqueViolation)
                    return DepartmentErrors.PathDuplicate();
                if (IsMoveConflict(postgresException))
                    return DepartmentErrors.RestoreConflict();
            }

            _logger.LogError(
                exception,
                "Failed to restore department. DepartmentId {DepartmentId}", departmentId.Value);
            return DatabaseErrors.OperationFailed("restore department");
        }
    }

    public async Task<Result<int, Error>> CleanupInactiveAsync(
        DateTime deletedBeforeUtc,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var transactionScopeResult = await _transactionManager.BeginTransactionAsync(cancellationToken);
        if (transactionScopeResult.IsFailure)
            return transactionScopeResult.Error;

        await using var transactionScope = transactionScopeResult.Value;

        try
        {
            var lockedSoftDeletedDepartments = await _dbContext.Database.SqlQuery<DepartmentCleanUpInfo>(
                $"""
                 select
                     d.id as "Id",
                     d.parent_id as "ParentId",
                     d.path::text as "Path",
                     d.depth As "Depth"
                 from departments d
                 where d.is_Active = false
                   and d.deleted_at < {deletedBeforeUtc}
                 order by d.depth desc, d.deleted_at, d.id
                 limit {batchSize}
                 for update skip locked
                 """).ToListAsync(cancellationToken);

            if (lockedSoftDeletedDepartments.Count == 0)
            {
                var rollbackResult = await transactionScope.RollbackAsync(cancellationToken);
                if (rollbackResult.IsFailure)
                    return rollbackResult.Error;

                return 0;
            }

            var cleanupPaths = lockedSoftDeletedDepartments
                .Select(l => l.Path)
                .Distinct()
                .ToArray();

            _ = await _dbContext.Database.SqlQuery<Guid>(
                $"""
                 SELECT d.id AS "Value"
                 FROM departments AS d
                 WHERE EXISTS (
                     select 1
                     from unnest({cleanupPaths}::text[]) as cleanup(path)
                     where d.path <@ cleanup.Path::ltree
                 )
                 ORDER BY d.id
                 FOR UPDATE OF d
                 """).ToListAsync(cancellationToken);

            foreach (var lockedDepartment in lockedSoftDeletedDepartments)
            {
                var path = lockedDepartment.Path;

                var newPathElements = lockedDepartment.Path.Split('.');
                var newPath = string.Join('.', newPathElements[..^1]);

                _ = await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"""
                     UPDATE departments
                     SET
                         parent_id = CASE
                            WHEN parent_id = {lockedDepartment.Id}
                            THEN {lockedDepartment.ParentId}
                            ELSE parent_id
                        END,
                        path = {newPath}::ltree || subpath(path, nlevel({path}::ltree)),
                        updated_at = now(),
                        depth = (depth - 1)::smallint
                     WHERE path <@ {path}::ltree and id <> {lockedDepartment.Id}
                     """, cancellationToken);
            }

            var departmentIds = lockedSoftDeletedDepartments
                .Select(d => d.Id)
                .ToArray();

            var deletedDepartmentsCount = await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 DELETE FROM departments
                 WHERE id = ANY({departmentIds})
                 """,
                cancellationToken);

            var commitResult = await transactionScope.CommitAsync(cancellationToken);
            if (commitResult.IsFailure)
                return commitResult.Error;

            return deletedDepartmentsCount;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "Failed to clean up soft deleted departments.");

            var rollbackResult = await transactionScope.RollbackAsync(cancellationToken);
            if (rollbackResult.IsFailure)
                return rollbackResult.Error;

            return DatabaseErrors.OperationFailed("clean up inactive departments");
        }
    }

    public async Task<Result<Department, Error>> AddAsync(
        Department department,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(department);

        _logger.LogDebug(
            "Adding department {DepartmentId} to change tracker",
            department.Id.Value);

        await _dbContext.Departments.AddAsync(department, cancellationToken);

        return department;
    }

    private static bool IsMoveConflict(PostgresException exception) =>
        exception.SqlState is
            PostgresErrorCodes.DeadlockDetected or
            PostgresErrorCodes.SerializationFailure or
            PostgresErrorCodes.LockNotAvailable;

    private sealed record DepartmentMoveInfo(
        Guid Id,
        Guid? ParentId,
        string Slug,
        string Path,
        short Depth,
        bool IsActive,
        DateTime UpdatedAt,
        DateTime? DeletedAt);

    private sealed record DepartmentCleanUpInfo(
        Guid Id,
        Guid? ParentId,
        string Path,
        short Depth);
}
