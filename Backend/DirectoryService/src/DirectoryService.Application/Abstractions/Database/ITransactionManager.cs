using CSharpFunctionalExtensions;
using DirectoryService.Domain.Shared;

namespace DirectoryService.Application.Abstractions.Database;

public interface ITransactionManager
{
    Task<Result<ITransactionScope, Error>> BeginTransactionAsync(
        CancellationToken cancellationToken = default);

    Task<UnitResult<Error>> SaveChangesAsync(
        string entityName,
        object? context = null,
        string? uniqueViolationMessage = null,
        CancellationToken cancellationToken = default);
}
