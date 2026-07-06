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

            var existingLocationIds = await _dbContext.Locations
                .AsNoTracking()
                .Where(location => location.IsActive && requestedLocationIds.Contains(location.Id))
                .Select(location => location.Id)
                .ToArrayAsync(cancellationToken);

            IReadOnlyCollection<LocationId> missingLocationIds = requestedLocationIds
                .Except(existingLocationIds)
                .ToArray();

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
