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

public sealed class LocationRepository : ILocationRepository
{
    private readonly DirectoryServiceDbContext _dbContext;
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<LocationRepository> _logger;

    public LocationRepository(
        DirectoryServiceDbContext dbContext,
        ITransactionManager transactionManager,
        ILogger<LocationRepository> logger)
    {
        _dbContext = dbContext;
        _transactionManager = transactionManager;
        _logger = logger;
    }

    public async Task<Result<bool, Error>> NameExistsAsync(
        string name,
        CancellationToken cancellationToken)
    {
        return await NameExistsInternalAsync(name, null, cancellationToken);
    }

    public async Task<Result<bool, Error>> NameExistsAsync(
        string name,
        LocationId excludedLocationId,
        CancellationToken cancellationToken)
    {
        return await NameExistsInternalAsync(name, excludedLocationId, cancellationToken);
    }

    public async Task<Result<Location, Error>> GetActiveByIdAsync(
        LocationId locationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var location = await _dbContext.Locations
                .FirstOrDefaultAsync(
                    item => item.Id == locationId && item.IsActive,
                    cancellationToken);

            return location is null
                ? LocationErrors.NotFound(locationId.Value, nameof(locationId))
                : location;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to load active location. LocationId {LocationId}",
                locationId.Value);

            return DatabaseErrors.OperationFailed("load active location");
        }
    }

    public async Task<Result<Location, Error>> AddAsync(Location location, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(location);

        _logger.LogDebug(
            "Adding location {LocationId} to change tracker",
            location.Id.Value);

        await _dbContext.Locations.AddAsync(location, cancellationToken);

        return location;
    }

    public UnitResult<Error> Delete(Location location)
    {
        ArgumentNullException.ThrowIfNull(location);

        location.SoftDelete();

        _logger.LogDebug(
            "Location marked as inactive. LocationId {LocationId}",
            location.Id.Value);

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
                     FROM locations
                     WHERE is_active = FALSE
                       AND deleted_at < {deletedBeforeUtc}
                     ORDER BY deleted_at, id
                     LIMIT {batchSize}
                     FOR UPDATE SKIP LOCKED
                 )
                 DELETE FROM locations AS location
                 USING cleanup_batch
                 WHERE location.id = cleanup_batch.id
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
            _logger.LogError(exception, "Failed to clean up soft deleted locations.");

            var rollbackResult = await transaction.RollbackAsync(cancellationToken);
            if (rollbackResult.IsFailure)
                return rollbackResult.Error;

            return DatabaseErrors.OperationFailed("clean up inactive locations");
        }
    }

    private async Task<Result<bool, Error>> NameExistsInternalAsync(
        string name,
        LocationId? excludedLocationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var normalizedName = name.Trim();

            return await _dbContext.Locations
                .AsNoTracking()
                .AnyAsync(
                    location =>
                        location.Name.Value == normalizedName &&
                        (excludedLocationId == null || location.Id != excludedLocationId),
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
                "Failed to check location name uniqueness. LocationName {LocationName}",
                name);

            return DatabaseErrors.OperationFailed("check location name uniqueness");
        }
    }
}
