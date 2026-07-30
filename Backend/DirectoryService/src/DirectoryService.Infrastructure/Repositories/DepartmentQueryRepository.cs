using CSharpFunctionalExtensions;
using Dapper;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Contracts.Departments;
using DirectoryService.Domain.Shared;
using DirectoryService.Infrastructure.Errors;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace DirectoryService.Infrastructure.Repositories;

public sealed class DepartmentQueryRepository : IDepartmentQueryRepository
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<DepartmentQueryRepository> _logger;

    public DepartmentQueryRepository(
        NpgsqlDataSource dataSource,
        ILogger<DepartmentQueryRepository> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<TopDepartmentByPositionsDto>, Error>> GetTopByPositionsAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var sql = BuildSql();

            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

            var command = new CommandDefinition(
                sql,
                cancellationToken: cancellationToken);

            var departments = await connection.QueryAsync<TopDepartmentByPositionsDto>(command);

            return Result.Success<IReadOnlyList<TopDepartmentByPositionsDto>, Error>(
                departments.AsList());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to load top departments by positions count");

            return DatabaseErrors.OperationFailed(
                "load top departments by positions count");
        }
    }

    private static string BuildSql()
    {
        // TODO: Return a query with Id, Name, Identifier and PositionsCount columns.
        throw new NotImplementedException("Implement the top departments Dapper query.");
    }
}
