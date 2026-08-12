using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Database;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Domain.Entities;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.Shared;
using DirectoryService.Infrastructure.Database;
using DirectoryService.Infrastructure.Errors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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

    public async Task<Result<bool, Error>> IdentifierExistsAsync(
        string identifier,
        CancellationToken cancellationToken)
    {
        try
        {
            var normalizedIdentifier = identifier.Trim();

            return await _dbContext.Departments
                .AsNoTracking()
                .AnyAsync(
                    department => department.Identifier.Value == normalizedIdentifier,
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
                "Failed to check department identifier uniqueness. DepartmentIdentifier {DepartmentIdentifier}",
                identifier);

            return DatabaseErrors.OperationFailed("check department identifier uniqueness");
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
                return GeneralErrors.NotFound("Department does not exist or is inactive.");

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

    public async Task<UnitResult<Error>> MoveParentAsync(
        DepartmentId departmentId,
        DepartmentId? parentId,
        CancellationToken cancellationToken)
    {
        var transactionScopeResult = await _transactionManager.BeginTransactionAsync(cancellationToken);
        if (transactionScopeResult.IsFailure)
            return UnitResult.Failure(transactionScopeResult.Error);

        await using var transactionScope = transactionScopeResult.Value;

        try
        {
            var department = await _dbContext.Database
                .SqlQuery<DepartmentMoveInfo>($"""
                                               SELECT
                                                   id AS "Id",
                                                   identifier AS "Identifier",
                                                   path::text AS "Path",
                                                   depth AS "Depth",
                                                   is_active AS "IsActive"
                                               FROM departments
                                               WHERE id = {departmentId.Value} AND is_active = TRUE
                                               FOR UPDATE
                                               """)
                .SingleOrDefaultAsync(cancellationToken);

            if (department is null)
            {
                var rollbackResult = await transactionScope.RollbackAsync(cancellationToken);
                if (rollbackResult.IsFailure)
                    return rollbackResult;

                return GeneralErrors.NotFound("Department is not found.", nameof(departmentId));
            }

            await _dbContext.Database
                .SqlQuery<DepartmentMoveInfo>($"""
                                               SELECT
                                                   id AS "Id",
                                                   identifier AS "Identifier",
                                                   path::text AS "Path",
                                                   depth AS "Depth",
                                                   is_active AS "IsActive"
                                               FROM departments
                                               WHERE path <@ {department.Path}::ltree
                                               ORDER BY id
                                               FOR UPDATE
                                               """)
                .ToListAsync(cancellationToken);

            DepartmentMoveInfo? parent = null;
            if (parentId is not null)
            {
                parent = await _dbContext.Database
                    .SqlQuery<DepartmentMoveInfo>($"""
                                                   SELECT
                                                       id AS "Id",
                                                       identifier AS "Identifier",
                                                       path::text AS "Path",
                                                       depth AS "Depth",
                                                       is_active AS "IsActive"
                                                   FROM departments
                                                   WHERE id = {parentId.Value} AND is_active = TRUE
                                                   FOR UPDATE
                                                   """)
                    .SingleOrDefaultAsync(cancellationToken);

                if (parent is null)
                {
                    var rollbackResult = await transactionScope.RollbackAsync(cancellationToken);
                    if (rollbackResult.IsFailure)
                        return rollbackResult;

                    return GeneralErrors.NotFound("Department parent is not found.", nameof(parentId));
                }
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
            {
                var rollbackResult = await transactionScope.RollbackAsync(cancellationToken);
                if (rollbackResult.IsFailure)
                    return rollbackResult;

                return GeneralErrors.Conflict(
                    "Cannot move department under its own descendant.",
                    nameof(parentId));
            }

            string oldPath = department.Path;
            string newPath = parent is null ? department.Identifier : parent.Path + "." + department.Identifier;
            Guid? newParentId = parent is null ? null : parent.Id;
            short newDepth = parent is null
                ? (short)0
                : checked((short)(parent.Depth + 1));
            int depthDelta = newDepth - department.Depth;

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
                    updated_at = now(),
                    depth = (depth + {depthDelta})::smallint
                 WHERE path <@ {oldPath}::ltree
                 """, cancellationToken);

            _logger.LogInformation(
                "Department parent was updated. DepartmentId {DepartmentId}. ParentId {ParentId}. UpdatedRows {UpdatedRows}",
                departmentId.Value,
                parentId?.Value,
                updatedRows);

            var commitResult = await transactionScope.CommitAsync(cancellationToken);
            if (commitResult.IsFailure)
                return commitResult;

            return UnitResult.Success<Error>();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            var rollbackResult = await transactionScope.RollbackAsync(cancellationToken);
            if (rollbackResult.IsFailure)
                return rollbackResult;

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
                          d.is_active = true
                    for update
                 """).SingleOrDefaultAsync(cancellationToken);

            if (lockedDepartmentId == Guid.Empty)
            {
                var transactionRollbackResult = await transactionScope.RollbackAsync(cancellationToken);
                if (transactionRollbackResult.IsFailure)
                    return transactionRollbackResult.Error;

                return GeneralErrors.NotFound("Department is not found.", nameof(departmentId));
            }

            var lockedDepartment = await _dbContext.Departments
                .FirstOrDefaultAsync(d => d.Id == departmentId && d.IsActive, cancellationToken);

            if (lockedDepartment is null)
            {
                var transactionRollbackResult = await transactionScope.RollbackAsync(cancellationToken);
                if (transactionRollbackResult.IsFailure)
                    return transactionRollbackResult.Error;

                return GeneralErrors.NotFound("Department is not found.", nameof(departmentId));
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
                 """,
                cancellationToken);

            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 UPDATE positions
                 SET is_active = FALSE,
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

    private sealed record DepartmentMoveInfo(
        Guid Id,
        string Identifier,
        string Path,
        short Depth,
        bool IsActive);
}
