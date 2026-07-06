using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions.Database;
using DirectoryService.Domain.Shared;
using DirectoryService.Infrastructure.Errors;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Infrastructure.Database;

public sealed class TransactionScope : ITransactionScope
{
    private readonly IDbContextTransaction _dbContextTransaction;
    private readonly ILogger<TransactionScope> _logger;

    public TransactionScope(
        IDbContextTransaction dbContextTransaction,
        ILogger<TransactionScope> logger)
    {
        _dbContextTransaction = dbContextTransaction;
        _logger = logger;
    }

    public async Task<UnitResult<Error>> CommitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContextTransaction.CommitAsync(cancellationToken);
            return UnitResult.Success<Error>();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to commit database transaction");
            return DatabaseErrors.OperationFailed("commit database transaction");
        }
    }

    public async Task<UnitResult<Error>> RollbackAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContextTransaction.RollbackAsync(cancellationToken);
            return UnitResult.Success<Error>();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to rollback database transaction");
            return DatabaseErrors.OperationFailed("rollback database transaction");
        }
    }

    public ValueTask DisposeAsync()
    {
        return _dbContextTransaction.DisposeAsync();
    }
}
