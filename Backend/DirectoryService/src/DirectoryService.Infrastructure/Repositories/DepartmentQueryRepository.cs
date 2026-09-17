using CSharpFunctionalExtensions;
using Dapper;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Contracts.Departments;
using DirectoryService.Domain.Errors;
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
        const string sql = """
                           select d.id as Id,
                                  d.name as Name,
                                  d.slug as Slug,
                                  count(p.id) as PositionsCount
                           from departments as d
                           left join department_positions as dp on d.id = dp.department_id
                           left join positions p on p.id = dp.position_id and p.is_active = true
                           where d.is_active = true
                           group by d.id
                           order by PositionsCount desc, d.id
                           limit 5
                           """;

        try
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
            var departments = (await connection.QueryAsync<TopDepartmentByPositionsDto>(command)).AsList();

            _logger.LogInformation(
                "Top departments by positions count loaded. ItemCount {ItemCount}",
                departments.Count);

            return departments;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to load top departments by positions count");
            return DatabaseErrors.OperationFailed("load top departments by positions count");
        }
    }

    public async Task<Result<IReadOnlyList<DepartmentTreeNodeDto>, Error>> GetTreeAsync(
        CancellationToken cancellationToken)
    {
        const string sql = """
                           select d.id as Id,
                                  d.name as Name,
                                  d.slug as Slug,
                                  d.path::text as Path,
                                  d.depth as Depth,
                                  exists(
                                      select 1
                                      from departments child
                                      where child.parent_id = d.id
                                        and child.is_active = true
                                  ) as HasChildren
                           from departments d
                           where d.parent_id is null
                             and d.is_active = true
                           order by d.name, d.id
                           """;

        return await QueryTreeNodesAsync(sql, null, "load department tree", cancellationToken);
    }

    public async Task<Result<IReadOnlyList<DepartmentTreeNodeDto>, Error>> GetChildrenAsync(
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        const string sql = """
                           select exists(
                               select 1
                               from departments
                               where id = @DepartmentId
                                 and is_active = true
                           );

                           select child.id as Id,
                                  child.name as Name,
                                  child.slug as Slug,
                                  child.path::text as Path,
                                  child.depth as Depth,
                                  exists(
                                      select 1
                                      from departments nested
                                      where nested.parent_id = child.id
                                        and nested.is_active = true
                                  ) as HasChildren
                           from departments child
                           where child.parent_id = @DepartmentId
                             and child.is_active = true
                           order by child.name, child.id
                           """;

        try
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            var command = new CommandDefinition(
                sql,
                new { DepartmentId = departmentId },
                cancellationToken: cancellationToken);

            using var result = await connection.QueryMultipleAsync(command);
            var departmentExists = await result.ReadSingleAsync<bool>();
            if (!departmentExists)
                return DepartmentErrors.NotFound(departmentId, nameof(departmentId));

            var children = (await result.ReadAsync<DepartmentTreeNodeDto>()).AsList();
            return children;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to load department children. DepartmentId {DepartmentId}",
                departmentId);

            return DatabaseErrors.OperationFailed("load department children");
        }
    }

    public async Task<Result<IReadOnlyList<DepartmentTreeNodeDto>, Error>> GetAncestorsAsync(
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        const string sql = """
                           select exists(
                               select 1
                               from departments
                               where id = @DepartmentId
                                 and is_active = true
                           );

                           with target as (
                               select path
                               from departments
                               where id = @DepartmentId
                                 and is_active = true
                           )
                           select ancestor.id as Id,
                                  ancestor.name as Name,
                                  ancestor.slug as Slug,
                                  ancestor.path::text as Path,
                                  ancestor.depth as Depth,
                                  exists(
                                      select 1
                                      from departments child
                                      where child.parent_id = ancestor.id
                                        and child.is_active = true
                                  ) as HasChildren
                           from target
                           join departments ancestor
                             on ancestor.path @> target.path
                            and ancestor.id <> @DepartmentId
                            and ancestor.is_active = true
                           order by ancestor.depth, ancestor.id
                           """;

        try
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            var command = new CommandDefinition(
                sql,
                new { DepartmentId = departmentId },
                cancellationToken: cancellationToken);

            using var result = await connection.QueryMultipleAsync(command);
            var departmentExists = await result.ReadSingleAsync<bool>();
            if (!departmentExists)
                return DepartmentErrors.NotFound(departmentId, nameof(departmentId));

            var ancestors = (await result.ReadAsync<DepartmentTreeNodeDto>()).AsList();
            return ancestors;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to load department ancestors. DepartmentId {DepartmentId}",
                departmentId);

            return DatabaseErrors.OperationFailed("load department ancestors");
        }
    }

    public async Task<Result<IReadOnlyList<DepartmentTreeSearchResultDto>, Error>> SearchTreeAsync(
        string search,
        CancellationToken cancellationToken)
    {
        const string sql = """
                           with matches as (
                               select d.id,
                                      d.name,
                                      d.slug,
                                      d.path,
                                      d.depth
                               from departments d
                               where d.is_active = true
                                 and d.name ilike @Search
                           )
                           select match.id as MatchId,
                                  match.name as MatchName,
                                  match.slug as MatchSlug,
                                  match.path::text as MatchPath,
                                  match.depth as MatchDepth,
                                  exists(
                                      select 1
                                      from departments child
                                      where child.parent_id = match.id
                                        and child.is_active = true
                                  ) as MatchHasChildren,
                                  ancestor.id as AncestorId,
                                  ancestor.name as AncestorName,
                                  ancestor.slug as AncestorSlug,
                                  ancestor.path::text as AncestorPath,
                                  ancestor.depth as AncestorDepth,
                                  case
                                      when ancestor.id is null then false
                                      else exists(
                                          select 1
                                          from departments child
                                          where child.parent_id = ancestor.id
                                            and child.is_active = true
                                      )
                                  end as AncestorHasChildren
                           from matches match
                           left join departments ancestor
                             on ancestor.path @> match.path
                            and ancestor.id <> match.id
                            and ancestor.is_active = true
                           order by match.name, match.id, ancestor.depth, ancestor.id
                           """;

        try
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            var command = new CommandDefinition(
                sql,
                new { Search = $"%{search}%" },
                cancellationToken: cancellationToken);

            var rows = (await connection.QueryAsync<DepartmentTreeSearchRow>(command)).AsList();
            var results = rows
                .GroupBy(row => row.MatchId)
                .Select(group =>
                {
                    var match = group.First();
                    var department = new DepartmentTreeNodeDto(
                        match.MatchId,
                        match.MatchName,
                        match.MatchSlug,
                        match.MatchPath,
                        match.MatchDepth,
                        match.MatchHasChildren);

                    var ancestors = group
                        .Where(row => row.AncestorId.HasValue)
                        .OrderBy(row => row.AncestorDepth)
                        .Select(row => new DepartmentTreeNodeDto(
                            row.AncestorId!.Value,
                            row.AncestorName!,
                            row.AncestorSlug!,
                            row.AncestorPath!,
                            row.AncestorDepth!.Value,
                            row.AncestorHasChildren))
                        .ToList();

                    return new DepartmentTreeSearchResultDto(department, ancestors);
                })
                .ToList();

            return results;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to search department tree. Search {Search}", search);
            return DatabaseErrors.OperationFailed("search department tree");
        }
    }

    private async Task<Result<IReadOnlyList<DepartmentTreeNodeDto>, Error>> QueryTreeNodesAsync(
        string sql,
        object? parameters,
        string operation,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            var command = new CommandDefinition(
                sql,
                parameters,
                cancellationToken: cancellationToken);

            var nodes = (await connection.QueryAsync<DepartmentTreeNodeDto>(command)).AsList();
            return nodes;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to {Operation}", operation);
            return DatabaseErrors.OperationFailed(operation);
        }
    }

    private sealed record DepartmentTreeSearchRow(
        Guid MatchId,
        string MatchName,
        string MatchSlug,
        string MatchPath,
        short MatchDepth,
        bool MatchHasChildren,
        Guid? AncestorId,
        string? AncestorName,
        string? AncestorSlug,
        string? AncestorPath,
        short? AncestorDepth,
        bool AncestorHasChildren);
}
