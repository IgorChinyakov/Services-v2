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

namespace DirectoryService.Infrastructure.Repositories;

public sealed class PositionRepository : IPositionRepository
{
    private readonly DirectoryServiceDbContext _dbContext;
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<PositionRepository> _logger;

    public PositionRepository(
        DirectoryServiceDbContext dbContext,
        ITransactionManager transactionManager,
        ILogger<PositionRepository> logger)
    {
        _dbContext = dbContext;
        _transactionManager = transactionManager;
        _logger = logger;
    }

    public async Task<Result<bool, Error>> ActiveNameExistsAsync(
        string name,
        CancellationToken cancellationToken)
    {
        return await ActiveNameExistsInternalAsync(name, null, cancellationToken);
    }

    public async Task<Result<bool, Error>> ActiveNameExistsAsync(
        string name,
        PositionId excludedPositionId,
        CancellationToken cancellationToken)
    {
        return await ActiveNameExistsInternalAsync(name, excludedPositionId, cancellationToken);
    }

    public async Task<Result<Position, Error>> GetActiveByIdAsync(
        PositionId positionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var position = await _dbContext.Positions
                .FirstOrDefaultAsync(
                    item => item.Id == positionId && item.IsActive,
                    cancellationToken);

            return position is null
                ? PositionErrors.NotFound(positionId.Value, nameof(positionId))
                : position;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to load active position. PositionId {PositionId}",
                positionId.Value);

            return DatabaseErrors.OperationFailed("load active position");
        }
    }

    public async Task<Result<IReadOnlyCollection<DepartmentId>, Error>> GetMissingActiveDepartmentIdsAsync(
        IReadOnlyCollection<DepartmentId> departmentIds,
        CancellationToken cancellationToken)
    {
        try
        {
            var requestedDepartmentIds = departmentIds.Distinct().ToArray();
            var missingDepartmentIds = new List<DepartmentId>();

            foreach (var departmentId in requestedDepartmentIds)
            {
                var exists = await _dbContext.Departments
                    .AsNoTracking()
                    .AnyAsync(
                        department => department.IsActive && department.Id == departmentId,
                        cancellationToken);

                if (!exists)
                    missingDepartmentIds.Add(departmentId);
            }

            return Result.Success<IReadOnlyCollection<DepartmentId>, Error>(missingDepartmentIds);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to check active departments. DepartmentIds {@DepartmentIds}",
                departmentIds.Select(id => id.Value));

            return DatabaseErrors.OperationFailed("check active departments");
        }
    }

    public async Task<Result<Position, Error>> AddAsync(
        Position position,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(position);

        _logger.LogDebug(
            "Adding position {PositionId} to change tracker",
            position.Id.Value);

        await _dbContext.Positions.AddAsync(position, cancellationToken);

        return position;
    }

    public UnitResult<Error> Delete(Position position)
    {
        ArgumentNullException.ThrowIfNull(position);

        position.SoftDelete();

        _logger.LogDebug(
            "Position marked as inactive. PositionId {PositionId}",
            position.Id.Value);

        return UnitResult.Success<Error>();
    }

    public async Task<Result<int, Error>> CleanupInactiveAsync(
        DateTime deletedBeforeUtc,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var transactionResult = await _transactionManager.BeginTransactionAsync(cancellationToken);
        if (transactionResult.IsFailure)
            return transactionResult.Error;

        await using var transaction = transactionResult.Value;

        try
        {
            var deletedCount = await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 WITH cleanup_batch AS (
                     SELECT id
                     FROM positions
                     WHERE is_active = FALSE
                       AND deleted_at < {deletedBeforeUtc}
                     ORDER BY deleted_at, id
                     LIMIT {batchSize}
                     FOR UPDATE SKIP LOCKED
                 )
                 DELETE FROM positions AS position
                 USING cleanup_batch
                 WHERE position.id = cleanup_batch.id
                 """,
                cancellationToken);

            var commitResult = await transaction.CommitAsync(cancellationToken);
            if (commitResult.IsFailure)
                return commitResult.Error;

            return deletedCount;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to clean up soft deleted positions.");

            var rollbackResult = await transaction.RollbackAsync(cancellationToken);
            if (rollbackResult.IsFailure)
                return rollbackResult.Error;

            return DatabaseErrors.OperationFailed("clean up inactive positions");
        }
    }

    private async Task<Result<bool, Error>> ActiveNameExistsInternalAsync(
        string name,
        PositionId? excludedPositionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var normalizedName = name.Trim();

            return await _dbContext.Positions
                .AsNoTracking()
                .AnyAsync(
                    position =>
                        position.IsActive &&
                        position.Name.Value == normalizedName &&
                        (excludedPositionId == null || position.Id != excludedPositionId),
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
                "Failed to check active position name uniqueness. PositionName {PositionName}",
                name);

            return DatabaseErrors.OperationFailed("check active position name uniqueness");
        }
    }
}
