using CSharpFunctionalExtensions;
using DirectoryService.Domain.Shared;

namespace DirectoryService.Application.Abstractions.Database;

public interface ITransactionScope : IAsyncDisposable
{
    Task<UnitResult<Error>> CommitAsync(CancellationToken cancellationToken = default);

    Task<UnitResult<Error>> RollbackAsync(CancellationToken cancellationToken = default);
}
