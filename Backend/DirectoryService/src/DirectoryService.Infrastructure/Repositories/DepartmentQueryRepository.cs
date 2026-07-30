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

            return departments.AsList();
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
        return """
               select d.id                  as Id,
                      d.name                as Name,
                      d.identifier          as Identifier,
                      count(p.id) as PositionsCount
               from departments as d
                        left join department_positions as dp on d.id = dp.department_id
                        left join positions p on p.id = dp.position_id and p.is_active = true
               where d.is_active = true
               group by d.id
               order by PositionsCount desc, d.id
               limit 5
               """;
    }
}
