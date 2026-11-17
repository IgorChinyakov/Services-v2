using CSharpFunctionalExtensions;
using Dapper;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Contracts.Search;
using DirectoryService.Domain.Shared;
using DirectoryService.Infrastructure.Errors;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace DirectoryService.Infrastructure.Repositories;

public sealed class SearchQueryRepository(
    NpgsqlDataSource dataSource,
    ILogger<SearchQueryRepository> logger) : ISearchQueryRepository
{
    private const int RESULTS_PER_TYPE = 5;

    private const string SEARCH_SQL = """
        WITH location_matches AS (
            SELECT l.id, l.name, l.city, l.street, l.house, l.apartment, l.is_active
            FROM locations l
            WHERE l.deleted_at IS NULL AND l.is_active = TRUE
              AND l.name ILIKE @Pattern ESCAPE '\'
            ORDER BY l.name, l.id
            LIMIT @Limit
        ), department_matches AS (
            SELECT d.id, d.name, d.path, d.is_active
            FROM departments d
            WHERE d.deleted_at IS NULL
              AND d.name ILIKE @Pattern ESCAPE '\'
            ORDER BY d.name, d.id
            LIMIT @Limit
        ), position_matches AS (
            SELECT p.id, p.name, p.description, p.is_active
            FROM positions p
            WHERE p.deleted_at IS NULL AND p.is_active = TRUE
              AND p.name ILIKE @Pattern ESCAPE '\'
            ORDER BY p.name, p.id
            LIMIT @Limit
        )
        SELECT * FROM (
            SELECT 'location' AS "Type", l.id AS "Id", l.name AS "Title",
                   concat_ws(', ', l.city, l.street, l.house, nullif(l.apartment, '')) AS "Subtitle",
                   l.is_active AS "IsActive", NULL::uuid AS "DepartmentId"
            FROM location_matches l
            UNION ALL
            SELECT 'department', d.id, d.name, d.path::text, d.is_active, NULL::uuid
            FROM department_matches d
            UNION ALL
            SELECT 'position', p.id, p.name,
                   coalesce(context.subtitle, nullif(p.description, ''), ''),
                   p.is_active, context.department_id
            FROM position_matches p
            LEFT JOIN LATERAL (
                SELECT string_agg(d.name, ', ' ORDER BY d.name, d.id) AS subtitle,
                       (array_agg(d.id ORDER BY d.name, d.id))[1] AS department_id
                FROM (
                    SELECT d.id, d.name
                    FROM department_positions dp
                    JOIN departments d ON d.id = dp.department_id
                    WHERE dp.position_id = p.id AND d.deleted_at IS NULL AND d.is_active = TRUE
                    ORDER BY d.name, d.id
                    LIMIT 3
                ) d
            ) context ON TRUE
        ) results
        ORDER BY CASE "Type" WHEN 'location' THEN 0 WHEN 'department' THEN 1 ELSE 2 END,
                 "Title", "Id"
        """;

    public async Task<Result<IReadOnlyList<GlobalSearchResultDto>, Error>> SearchAsync(
        string search,
        CancellationToken cancellationToken)
    {
        // Treat LIKE wildcards as literal user input, while keeping SQL parameters bound.
        var escapedSearch = search.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            var command = new CommandDefinition(
                SEARCH_SQL,
                new { Pattern = $"%{escapedSearch}%", Limit = RESULTS_PER_TYPE },
                cancellationToken: cancellationToken);
            var results = (await connection.QueryAsync<GlobalSearchResultDto>(command)).AsList();
            return results;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to search directory entities");
            return DatabaseErrors.OperationFailed("search directory entities");
        }
    }
}
