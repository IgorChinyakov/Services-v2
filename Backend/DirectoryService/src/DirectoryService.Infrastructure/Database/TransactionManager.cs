using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Database;
using DirectoryService.Domain.Shared;
using DirectoryService.Infrastructure.Errors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace DirectoryService.Infrastructure.Database;

public sealed class TransactionManager : ITransactionManager
{
    private readonly DirectoryServiceDbContext _dbContext;
    private readonly ILogger<TransactionManager> _logger;
    private readonly ILogger<TransactionScope> _scopeLogger;

    public TransactionManager(
        DirectoryServiceDbContext dbContext,
        ILogger<TransactionManager> logger,
        ILogger<TransactionScope> scopeLogger)
    {
        _dbContext = dbContext;
        _logger = logger;
        _scopeLogger = scopeLogger;
    }

    public async Task<Result<ITransactionScope, Error>> BeginTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            return new TransactionScope(transaction, _scopeLogger);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to begin database transaction");
            return DatabaseErrors.OperationFailed("begin database transaction");
        }
    }

    public async Task<UnitResult<Error>> SaveChangesAsync(
        string entityName,
        object? context = null,
        string? uniqueViolationMessage = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return UnitResult.Success<Error>();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateException exception)
        {
            ClearChangeTracker();

            var error = MapDbUpdateException(exception, entityName, uniqueViolationMessage);
            var sqlState = (exception.InnerException as PostgresException)?.SqlState;

            _logger.LogError(
                exception,
                "Database update failed while saving {EntityName}. ErrorType {ErrorType}. SqlState {SqlState}. Context {@Context}",
                entityName,
                error.Type,
                sqlState,
                context);

            return error;
        }
        catch (TimeoutException exception)
        {
            ClearChangeTracker();

            _logger.LogError(
                exception,
                "Database timeout while saving {EntityName}. Context {@Context}",
                entityName,
                context);

            return DatabaseErrors.Timeout($"saving the {entityName}");
        }
        catch (NpgsqlException exception)
        {
            ClearChangeTracker();

            _logger.LogError(
                exception,
                "Database is unavailable while saving {EntityName}. Context {@Context}",
                entityName,
                context);

            return DatabaseErrors.Unavailable($"save the {entityName}");
        }
        catch (Exception exception)
        {
            ClearChangeTracker();

            _logger.LogError(
                exception,
                "Unexpected database error while saving {EntityName}. Context {@Context}",
                entityName,
                context);

            return DatabaseErrors.SaveFailed(entityName);
        }
    }

    private static Error MapDbUpdateException(
        DbUpdateException exception,
        string entityName,
        string? uniqueViolationMessage)
    {
        if (exception.InnerException is not PostgresException postgresException)
            return DatabaseErrors.SaveFailed(entityName);

        return DatabaseErrors.FromPostgresException(
            postgresException,
            entityName,
            uniqueViolationMessage);
    }

    private void ClearChangeTracker()
    {
        _dbContext.ChangeTracker.Clear();
    }
}
