using CSharpFunctionalExtensions;
using DirectoryService.Domain.Shared;
using DirectoryService.Infrastructure.Errors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace DirectoryService.Infrastructure.Database;

public sealed class DbOperationExecutor
{
    private readonly ILogger<DbOperationExecutor> _logger;

    public DbOperationExecutor(ILogger<DbOperationExecutor> logger)
    {
        _logger = logger;
    }

    public async Task<Result<TValue, Error>> ExecuteAsync<TValue>(
        Func<CancellationToken, Task<TValue>> operation,
        string operationName,
        object? context,
        CancellationToken cancellationToken)
    {
        try
        {
            return await operation(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is TimeoutException or NpgsqlException)
        {
            _logger.LogError(
                exception,
                "Database operation failed while {OperationName}. Context {@Context}",
                operationName,
                context);

            return DatabaseErrors.OperationFailed(operationName);
        }
    }

    public async Task<Result<TValue, Error>> ExecuteResultAsync<TValue>(
        Func<CancellationToken, Task<Result<TValue, Error>>> operation,
        string operationName,
        object? context,
        CancellationToken cancellationToken)
    {
        try
        {
            return await operation(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is TimeoutException or NpgsqlException)
        {
            _logger.LogError(
                exception,
                "Database operation failed while {OperationName}. Context {@Context}",
                operationName,
                context);

            return DatabaseErrors.OperationFailed(operationName);
        }
    }

    public async Task<Result<TEntity, Error>> ExecuteSaveAsync<TEntity>(
        Func<CancellationToken, Task<TEntity>> operation,
        Action detach,
        string entityName,
        object? context,
        CancellationToken cancellationToken,
        string? uniqueViolationMessage = null)
    {
        try
        {
            return await operation(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateException exception)
        {
            detach();

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
            detach();

            _logger.LogError(
                exception,
                "Database timeout while saving {EntityName}. Context {@Context}",
                entityName,
                context);

            return DatabaseErrors.Timeout($"saving the {entityName}");
        }
        catch (NpgsqlException exception)
        {
            detach();

            _logger.LogError(
                exception,
                "Database is unavailable while saving {EntityName}. Context {@Context}",
                entityName,
                context);

            return DatabaseErrors.Unavailable($"save the {entityName}");
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
}
