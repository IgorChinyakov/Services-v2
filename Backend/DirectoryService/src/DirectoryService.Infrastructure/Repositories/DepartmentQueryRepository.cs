using CSharpFunctionalExtensions;
using Dapper;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Application.Features.Departments.GetChildren;
using DirectoryService.Application.Features.Departments.GetRoots;
using DirectoryService.Contracts.Common;
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

            _logger.LogInformation("Loading top departments by positions count from database");

            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

            var command = new CommandDefinition(
                sql,
                cancellationToken: cancellationToken);

            var departments = await connection.QueryAsync<TopDepartmentByPositionsDto>(command);
            var departmentList = departments.AsList();

            _logger.LogInformation(
                "Top departments by positions count loaded from database. ItemCount: {ItemCount}",
                departmentList.Count);

            return departmentList;
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

    public async Task<Result<PagedList<RootDepartmentDto>, Error>> GetRootsAsync(
        GetRootDepartmentsQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var sql = BuildRootDepartmentsSql();
            var parameters = new
            {
                Offset = (query.Page - 1) * query.Size, RootLimit = query.Size, ChildLimit = query.Prefetch,
            };

            _logger.LogInformation(
                "Loading root departments from database. Page: {Page}, Size: {Size}, Prefetch: {Prefetch}",
                query.Page,
                query.Size,
                query.Prefetch);

            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

            var command = new CommandDefinition(
                sql,
                parameters,
                cancellationToken: cancellationToken);

            using var result = await connection.QueryMultipleAsync(command);

            var totalCount = await result.ReadSingleAsync<long>();
            var rows = (await result.ReadAsync<DepartmentTreeRow>()).AsList();
            var roots = MapRoots(rows);

            _logger.LogInformation(
                "Root departments loaded from database. RootCount: {RootCount}, TotalCount: {TotalCount}, Page: {Page}",
                roots.Count,
                totalCount,
                query.Page);

            return PagedList<RootDepartmentDto>.Create(
                roots,
                query.Page,
                query.Size,
                totalCount);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to load root departments. Page {Page}. Size {Size}. Prefetch {Prefetch}",
                query.Page,
                query.Size,
                query.Prefetch);

            return DatabaseErrors.OperationFailed("load root departments");
        }
    }

    public async Task<Result<PagedList<DepartmentNodeDto>, Error>> GetChildrenAsync(
        GetDepartmentChildrenQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var sql = BuildDepartmentChildrenSql();
            var parameters = new { query.ParentId, Offset = (query.Page - 1) * query.Size, PageSize = query.Size, };

            _logger.LogInformation(
                "Loading department children from database. ParentId: {ParentId}, Page: {Page}, Size: {Size}",
                query.ParentId,
                query.Page,
                query.Size);

            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

            var command = new CommandDefinition(
                sql,
                parameters,
                cancellationToken: cancellationToken);

            using var result = await connection.QueryMultipleAsync(command);

            var totalCount = await result.ReadSingleAsync<long>();
            var rows = (await result.ReadAsync<DepartmentTreeRow>()).AsList();
            var children = rows.Select(MapNode).ToList();

            _logger.LogInformation(
                "Department children loaded from database. ParentId: {ParentId}, ItemCount: {ItemCount}, " +
                "TotalCount: {TotalCount}, Page: {Page}",
                query.ParentId,
                children.Count,
                totalCount,
                query.Page);

            return PagedList<DepartmentNodeDto>.Create(
                children,
                query.Page,
                query.Size,
                totalCount);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to load department children. ParentId {ParentId}. Page {Page}. Size {Size}",
                query.ParentId,
                query.Page,
                query.Size);

            return DatabaseErrors.OperationFailed("load department children");
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

    private static string BuildRootDepartmentsSql()
    {
        return """
               select count(*)
               from departments
               where parent_id is null
                 and is_active = true;

               with roots as (select *
                              from departments
                              where parent_id is null
                              and is_active = true
                              order by created_at, id
                              limit @RootLimit offset @Offset)

               select r.id                               as Id,
                      r.parent_id                        as ParentId,
                      r.name                             as Name,
                      r.identifier                       as Identifier,
                      r.path::text                       as Path,
                      r.depth                            as Depth,
                      r.is_active                        as IsActive,
                      r.created_at                       as CreatedAt,
                      r.updated_at                       as UpdatedAt,
                      exists(select 1
                             from departments d
                             where d.parent_id = r.id and
                                   d.is_active = true
                             offset @ChildLimit limit 1) as HasMoreChildren
               from roots r

               union all

               select c.id                             as Id,
                      c.parent_id                      as ParentId,
                      c.name                           as Name,
                      c.identifier                     as Identifier,
                      c.path::text                     as Path,
                      c.depth                          as Depth,
                      c.is_active                      as IsActive,
                      c.created_at                     as CreatedAt,
                      c.updated_at                     as UpdatedAt,
                      exists(select 1
                             from departments d
                             where d.parent_id = c.id and
                                   d.is_active = true) as HasMoreChildren
               from roots r
                        cross join lateral (
                   select *
                   from departments c
                   where c.parent_id = r.id
                     and c.is_active = true
                   order by c.created_at, c.id
                   limit @ChildLimit
                   ) c
               """;
    }

    private static string BuildDepartmentChildrenSql()
    {
        return """
               select count(*)
               from departments
               where parent_id = @ParentId
                 and is_active = true;

               select c.id         as Id,
                      c.parent_id  as ParentId,
                      c.name       as Name,
                      c.identifier as Identifier,
                      c.path::text as Path,
                      c.depth      as Depth,
                      c.is_active  as IsActive,
                      c.created_at as CreatedAt,
                      c.updated_at as UpdatedAt,
                      exists(
                            select 1 from departments d
                            where d.is_active = true and
                            d.parent_id = c.id
                      ) as HasMoreChildren
               from departments c
               where parent_id = @ParentId
                 and is_active = true
               order by created_at, id
               offset @Offset
               limit @PageSize
               """;
    }

    private static IReadOnlyList<RootDepartmentDto> MapRoots(
        IReadOnlyCollection<DepartmentTreeRow> rows)
    {
        var childrenByParentId = rows
            .Where(row => row.ParentId.HasValue)
            .GroupBy(row => row.ParentId!.Value)
            .ToDictionary(group => group.Key, group => group.ToList());

        var roots = new List<RootDepartmentDto>();

        foreach (var root in rows.Where(row => row.ParentId is null))
        {
            var children = childrenByParentId.TryGetValue(root.Id, out var childRows)
                ? childRows.Select(MapNode).ToList()
                : [];

            roots.Add(MapRoot(root, children));
        }

        return roots;
    }

    private static RootDepartmentDto MapRoot(
        DepartmentTreeRow row,
        IReadOnlyList<DepartmentNodeDto> children)
    {
        return new RootDepartmentDto(
            row.Id,
            row.ParentId,
            row.Name,
            row.Identifier,
            row.Path,
            row.Depth,
            row.IsActive,
            row.CreatedAt,
            row.UpdatedAt,
            row.HasMoreChildren,
            children);
    }

    private static DepartmentNodeDto MapNode(DepartmentTreeRow row)
    {
        return new DepartmentNodeDto(
            row.Id,
            row.ParentId,
            row.Name,
            row.Identifier,
            row.Path,
            row.Depth,
            row.IsActive,
            row.CreatedAt,
            row.UpdatedAt,
            row.HasMoreChildren);
    }

    private sealed record DepartmentTreeRow(
        Guid Id,
        Guid? ParentId,
        string Name,
        string Identifier,
        string Path,
        short Depth,
        bool IsActive,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        bool HasMoreChildren);
}
