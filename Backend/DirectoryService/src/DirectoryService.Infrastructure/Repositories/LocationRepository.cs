using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Domain.Entities;
using DirectoryService.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace DirectoryService.Infrastructure.Repositories;

public sealed class LocationRepository : ILocationRepository
{
    private readonly DirectoryServiceDbContext _dbContext;
    private readonly ILogger<LocationRepository> _logger;

    public LocationRepository(
        DirectoryServiceDbContext dbContext,
        ILogger<LocationRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<Location, Error>> AddAsync(Location location, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(location);

        try
        {
            _logger.LogDebug(
                "Persisting location {LocationId} to database",
                location.Id.Value);

            await _dbContext.Locations.AddAsync(location, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Location {LocationId} successfully persisted to database",
                location.Id.Value);

            return location;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateException exception)
        {
            Detach(location);

            var error = MapDbUpdateException(exception);
            var sqlState = (exception.InnerException as PostgresException)?.SqlState;

            _logger.LogError(
                exception,
                "Database update failed while saving location {LocationId}. ErrorType {ErrorType}. SqlState {SqlState}",
                location.Id.Value,
                error.Type,
                sqlState);

            return error;
        }
        catch (TimeoutException exception)
        {
            Detach(location);

            _logger.LogError(
                exception,
                "Database timeout while saving location {LocationId}",
                location.Id.Value);

            return Error.Failure("The database timed out while saving the location.");
        }
        catch (NpgsqlException exception)
        {
            Detach(location);

            _logger.LogError(
                exception,
                "Database is unavailable while saving location {LocationId}",
                location.Id.Value);

            return Error.Failure("The database is unavailable. Failed to save the location.");
        }
    }

    private static Error MapDbUpdateException(DbUpdateException exception)
    {
        if (exception.InnerException is not PostgresException postgresException)
            return Error.Failure("Failed to save the location to the database.");

        return postgresException.SqlState switch
        {
            PostgresErrorCodes.UniqueViolation =>
                Error.Conflict("A location with the same unique values already exists."),

            PostgresErrorCodes.NotNullViolation =>
                Error.Validation(
                    "The location contains an empty value required by the database.",
                    postgresException.ColumnName),

            PostgresErrorCodes.StringDataRightTruncation =>
                Error.Validation(
                    "One of the location fields exceeds the maximum allowed length.",
                    postgresException.ColumnName),

            PostgresErrorCodes.CheckViolation =>
                Error.Validation("The location violates a database constraint."),

            PostgresErrorCodes.ForeignKeyViolation =>
                Error.Conflict("The location references data that does not exist."),

            _ => Error.Failure("Failed to save the location to the database."),
        };
    }

    private void Detach(Location location)
    {
        var entry = _dbContext.Entry(location);
        if (entry.State != EntityState.Detached)
            entry.State = EntityState.Detached;
    }
}
