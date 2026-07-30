using System.Data;
using System.Data.Common;
using System.Text;
using CSharpFunctionalExtensions;
using Dapper;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Application.Features.Locations.Get;
using DirectoryService.Contracts.Common;
using DirectoryService.Contracts.Locations;
using DirectoryService.Contracts.Locations.Sort;
using DirectoryService.Domain.Entities;
using DirectoryService.Domain.Shared;
using DirectoryService.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Infrastructure.Repositories;

public sealed class LocationRepository : ILocationRepository
{
    private readonly DirectoryServiceDbContext _dbContext;
    private readonly ILogger<LocationRepository> _logger;

    public LocationRepository(
        DirectoryServiceDbContext dbContext,
        ILogger<LocationRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<Location, Error>> AddAsync(Location location, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(location);

        _logger.LogDebug(
            "Adding location {LocationId} to change tracker",
            location.Id.Value);

        await _dbContext.Locations.AddAsync(location, cancellationToken);

        return location;
    }

    public async Task<PagedList<LocationDto>> GetFilteredWithPaginationAsync(
        GetLocationsQuery query,
        CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        var filtersSql = BuildFiltersSql(query, parameters);

        parameters.Add("PageSize", query.PageSize);
        parameters.Add("Offset", (query.Page - 1) * query.PageSize);

        try
        {
            var connection = _dbContext.Database.GetDbConnection();
            var locationsSql = BuildLocationsSql(query, filtersSql);

            var locations = await QueryLocationsAsync(
                connection,
                locationsSql,
                parameters,
                cancellationToken);

            var totalCount = await CountLocationsAsync(
                connection,
                filtersSql,
                parameters,
                cancellationToken);

            return PagedList<LocationDto>.Create(
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

            throw;
        }
    }

    private static string BuildFiltersSql(
        GetLocationsQuery query,
        DynamicParameters parameters)
    {
        var filtersSql = new StringBuilder("""
                                           from locations as l
                                           where 1 = 1
                                           """);

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
                                       from department_locations dl
                                       where dl.location_id = l.id
                                         and dl.department_id in ({string.Join(", ", placeholders)})
                                   )
                                   """);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            filtersSql.AppendLine("and l.name ilike @Search");
            parameters.Add("Search", $"%{query.Search.Trim()}%");
        }

        if (query.IsActive.HasValue)
        {
            filtersSql.AppendLine("and l.is_active = @IsActive");
            parameters.Add("IsActive", query.IsActive.Value);
        }

        return filtersSql.ToString();
    }

    private static string BuildLocationsSql(
        GetLocationsQuery query,
        string filtersSql)
    {
        var sortColumn = query.SortBy switch
        {
            LocationSortBy.Name => "l.name",
            LocationSortBy.CreatedAt => "l.created_at",
            _ => throw new ArgumentOutOfRangeException(nameof(query.SortBy)),
        };

        var sortDirection = query.SortDirection switch
        {
            SortDirection.Asc => "asc",
            SortDirection.Desc => "desc",
            _ => throw new ArgumentOutOfRangeException(nameof(query.SortDirection)),
        };

        return $"""
                select
                    l.id as Id,
                    l.name as Name,
                    l.time_zone as Timezone,
                    l.is_active as IsActive,
                    l.country as Country,
                    l.city as City,
                    l.street as Street,
                    l.building as Building
                {filtersSql}
                order by {sortColumn} {sortDirection}, l.id asc
                limit @PageSize
                offset @Offset
                """;
    }

    private static async Task<IReadOnlyList<LocationDto>> QueryLocationsAsync(
        DbConnection connection,
        string sql,
        DynamicParameters parameters,
        CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(
            commandText: sql,
            parameters: parameters,
            cancellationToken: cancellationToken);

        var locations = await connection.QueryAsync<LocationRow, AddressDto, LocationDto>(
            command,
            map: (locationRow, addressDto) => new LocationDto(
                locationRow.Id,
                locationRow.Name,
                addressDto,
                locationRow.Timezone,
                locationRow.IsActive),
            splitOn: "Country");

        return locations.ToList();
    }

    private static async Task<long> CountLocationsAsync(
        DbConnection connection,
        string filtersSql,
        DynamicParameters parameters,
        CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(
            commandText: $"select count(*) {filtersSql}",
            parameters: parameters,
            cancellationToken: cancellationToken);

        return await connection.ExecuteScalarAsync<long>(command);
    }

    private sealed record LocationRow(
        Guid Id,
        string Name,
        string Timezone,
        bool IsActive);
}
