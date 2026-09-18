using System.Data;
using System.Text;
using CSharpFunctionalExtensions;
using Dapper;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Application.Features.Locations.Get;
using DirectoryService.Contracts.Common;
using DirectoryService.Contracts.Locations;
using DirectoryService.Contracts.Locations.Sort;
using DirectoryService.Domain.Shared;
using DirectoryService.Infrastructure.Errors;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace DirectoryService.Infrastructure.Repositories;

public sealed class LocationQueryRepository : ILocationQueryRepository
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<LocationQueryRepository> _logger;

    public LocationQueryRepository(
        NpgsqlDataSource dataSource,
        ILogger<LocationQueryRepository> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<TopLocationByDepartmentsDto>, Error>> GetTopByDepartmentsAsync(
        CancellationToken cancellationToken)
    {
        const string sql = """
                           select
                               l.id as Id,
                               l.name as Name,
                               l.city as City,
                               l.street as Street,
                               l.house as House,
                               l.apartment as Apartment,
                               count(d.id)::bigint as DepartmentCount
                           from locations as l
                           left join department_locations as dl on dl.location_id = l.id
                           left join departments as d
                             on d.id = dl.department_id
                            and d.is_active = true
                           where l.is_active = true
                           group by
                               l.id,
                               l.name,
                               l.city,
                               l.street,
                               l.house,
                               l.apartment
                           order by DepartmentCount desc, l.id
                           limit 5
                           """;

        try
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
            var rows = (await connection.QueryAsync<TopLocationResultRow>(command)).AsList();

            var locations = rows
                .Select(row => new TopLocationByDepartmentsDto(
                    row.Id,
                    row.Name,
                    new AddressDto
                    {
                        City = row.City,
                        Street = row.Street,
                        House = row.House,
                        Apartment = row.Apartment,
                    },
                    row.DepartmentCount))
                .ToList();

            _logger.LogInformation(
                "Top locations by departments count loaded. ItemCount {ItemCount}",
                locations.Count);

            return locations;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to load top locations by departments count");
            return DatabaseErrors.OperationFailed("load top locations by departments count");
        }
    }

    public async Task<Result<PagedList<LocationListItemDto>, Error>> GetFilteredWithPaginationAsync(
        GetLocationsQuery query,
        CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        var filtersSql = BuildFiltersSql(query, parameters);

        parameters.Add("MinDepartmentCount", query.MinDepartmentCount, DbType.Int32);
        parameters.Add("IsActive", true, DbType.Boolean);
        parameters.Add("PageSize", query.PageSize, DbType.Int32);
        parameters.Add("Offset", (query.Page - 1) * query.PageSize, DbType.Int32);

        try
        {
            var locationsSql = BuildLocationsSql(query, filtersSql);

            _logger.LogInformation(
                "Loading locations from database. Page: {Page}, PageSize: {PageSize}, " +
                "Search: {Search}, DepartmentIdsCount: {DepartmentIdsCount}, " +
                "MinDepartmentCount: {MinDepartmentCount}, " +
                "SortBy: {SortBy}, SortDirection: {SortDirection}",
                query.Page,
                query.PageSize,
                query.Search,
                query.DepartmentIds?.Length ?? 0,
                query.MinDepartmentCount,
                query.SortBy,
                query.SortDirection);

            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

            var (locations, totalCount) = await QueryLocationsAsync(
                connection,
                locationsSql,
                parameters,
                cancellationToken);

            _logger.LogInformation(
                "Locations loaded from database. ItemCount: {ItemCount}, TotalCount: {TotalCount}, Page: {Page}",
                locations.Count,
                totalCount,
                query.Page);

            return PagedList<LocationListItemDto>.Create(
                locations,
                query.Page,
                query.PageSize,
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
                "Failed to receive locations from database");

            return DatabaseErrors.OperationFailed("load locations");
        }
    }

    private static string BuildFiltersSql(
        GetLocationsQuery query,
        DynamicParameters parameters)
    {
        var filtersSql = new StringBuilder("where 1 = 1");

        filtersSql.AppendLine();

        if (query.DepartmentIds is { Length: > 0 })
        {
            var placeholders = new List<string>(query.DepartmentIds.Length);

            for (var i = 0; i < query.DepartmentIds.Length; i++)
            {
                var parameterName = $"DepartmentId{i}";

                placeholders.Add($"@{parameterName}");
                parameters.Add(
                    parameterName,
                    query.DepartmentIds[i],
                    DbType.Guid);
            }

            filtersSql.AppendLine($"""
                                   and exists (
                                       select 1
                                       from department_locations filtered_dl
                                       join departments filtered_d
                                         on filtered_d.id = filtered_dl.department_id
                                        and filtered_d.is_active = true
                                       where filtered_dl.location_id = l.id
                                         and filtered_dl.department_id in ({string.Join(", ", placeholders)})
                                   )
                                   """);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            filtersSql.AppendLine("and l.name ilike @Search");
            parameters.Add("Search", $"%{query.Search.Trim()}%");
        }

        filtersSql.AppendLine("and l.is_active = @IsActive");

        return filtersSql.ToString();
    }

    private static string BuildLocationsSql(
        GetLocationsQuery query,
        string filtersSql)
    {
        var cteSortColumn = query.SortBy switch
        {
            LocationSortBy.Name => "fl.name",
            LocationSortBy.CreatedAt => "fl.created_at",
            LocationSortBy.DepartmentCount => "fl.department_count",
            _ => throw new ArgumentOutOfRangeException(nameof(query.SortBy)),
        };

        var resultSortColumn = query.SortBy switch
        {
            LocationSortBy.Name => "pl.name",
            LocationSortBy.CreatedAt => "pl.created_at",
            LocationSortBy.DepartmentCount => "pl.department_count",
            _ => throw new ArgumentOutOfRangeException(nameof(query.SortBy)),
        };

        var sortDirection = query.SortDirection switch
        {
            SortDirection.Asc => "asc",
            SortDirection.Desc => "desc",
            _ => throw new ArgumentOutOfRangeException(nameof(query.SortDirection)),
        };

        return $"""
                with locations_with_department_count as (
                    select
                        l.id,
                        l.name,
                        l.is_active,
                        l.created_at,
                        l.city,
                        l.street,
                        l.house,
                        l.apartment,
                        count(d.id)::bigint as department_count
                    from locations as l
                    left join department_locations as dl on dl.location_id = l.id
                    left join departments as d
                      on d.id = dl.department_id
                     and d.is_active = true
                    {filtersSql}
                    group by
                        l.id,
                        l.name,
                        l.is_active,
                        l.created_at,
                        l.city,
                        l.street,
                        l.house,
                        l.apartment
                ),
                filtered_locations as (
                    select
                        *
                    from locations_with_department_count
                    where @MinDepartmentCount is null
                       or department_count >= @MinDepartmentCount
                ),
                paged_locations as (
                    select
                        fl.*
                    from filtered_locations as fl
                    order by {cteSortColumn} {sortDirection}, fl.id asc
                    limit @PageSize
                    offset @Offset
                )
                select
                    pl.id as Id,
                    pl.name as Name,
                    pl.is_active as IsActive,
                    pl.created_at as CreatedAt,
                    pl.department_count as DepartmentCount,
                    pl.city as City,
                    pl.street as Street,
                    pl.house as House,
                    pl.apartment as Apartment,
                    totals.total_count as TotalCount
                from (
                    select count(*)::bigint as total_count
                    from filtered_locations
                ) as totals
                left join paged_locations as pl on true
                order by {resultSortColumn} {sortDirection} nulls last, pl.id asc
                """;
    }

    private static async Task<(IReadOnlyList<LocationListItemDto> Locations, long TotalCount)> QueryLocationsAsync(
        NpgsqlConnection connection,
        string sql,
        DynamicParameters parameters,
        CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(
            commandText: sql,
            parameters: parameters,
            cancellationToken: cancellationToken);

        var rows = (await connection.QueryAsync<LocationResultRow>(command)).AsList();
        var totalCount = rows.Count == 0 ? 0 : rows[0].TotalCount;

        var locations = rows
            .Where(row => row.Id.HasValue)
            .Select(row => new LocationListItemDto(
                row.Id!.Value,
                row.Name!,
                new AddressDto
                {
                    City = row.City!,
                    Street = row.Street!,
                    House = row.House!,
                    Apartment = row.Apartment!,
                },
                row.IsActive!.Value,
                row.CreatedAt!.Value,
                row.DepartmentCount!.Value))
            .ToList();

        return (locations, totalCount);
    }

    private sealed record LocationResultRow(
        Guid? Id,
        string? Name,
        bool? IsActive,
        DateTime? CreatedAt,
        long? DepartmentCount,
        string? City,
        string? Street,
        string? House,
        string? Apartment,
        long TotalCount);

    private sealed record TopLocationResultRow(
        Guid Id,
        string Name,
        string City,
        string Street,
        string House,
        string Apartment,
        long DepartmentCount);
}
