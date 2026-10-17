using System.Data;
using System.Globalization;
using System.Text;
using CSharpFunctionalExtensions;
using Dapper;
using DirectoryService.Application.Abstractions.Repositories;
using DirectoryService.Application.Features.Positions.Get;
using DirectoryService.Contracts.Positions;
using DirectoryService.Domain.Shared;
using DirectoryService.Infrastructure.Errors;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace DirectoryService.Infrastructure.Repositories;

public sealed class PositionQueryRepository : IPositionQueryRepository
{
    private const string POSITIONS_SQL = """
                                         select
                                             p.id as "Id",
                                             p.name as "Name",
                                             p.description as "Description",
                                             p.created_at as "CreatedAt"
                                         from positions as p
                                         where p.is_active = true
                                           and (
                                               @CursorCreatedAt::timestamptz is null
                                               or (p.created_at, p.id) >
                                                  (@CursorCreatedAt::timestamptz, @CursorId::uuid)
                                           )
                                           and (
                                               @DepartmentId::uuid is null
                                               or exists (
                                                   select 1
                                                   from department_positions as dp
                                                   join departments as d on d.id = dp.department_id
                                                   where dp.position_id = p.id
                                                     and dp.department_id = @DepartmentId::uuid
                                                     and d.is_active = true
                                               )
                                           )
                                         order by p.created_at, p.id
                                         limit @SizePlusOne
                                         """;

    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<PositionQueryRepository> _logger;

    public PositionQueryRepository(
        NpgsqlDataSource dataSource,
        ILogger<PositionQueryRepository> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    public async Task<Result<PositionCursorPageDto, Error>> GetAsync(
        GetPositionsQuery query,
        CancellationToken cancellationToken)
    {
        var cursorResult = ParseCursor(query.Cursor, query.DepartmentId);
        if (cursorResult.IsFailure)
            return cursorResult.Error;

        var parameters = new DynamicParameters();
        parameters.Add("CursorCreatedAt", cursorResult.Value?.CreatedAt, DbType.DateTime);
        parameters.Add("CursorId", cursorResult.Value?.Id, DbType.Guid);
        parameters.Add("DepartmentId", query.DepartmentId, DbType.Guid);
        parameters.Add("SizePlusOne", query.Size + 1, DbType.Int32);

        try
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            var command = new CommandDefinition(
                POSITIONS_SQL,
                parameters,
                cancellationToken: cancellationToken);

            var rows = (await connection.QueryAsync<PositionRow>(command)).AsList();
            var hasNextPage = rows.Count > query.Size;
            var pageRows = rows.Take(query.Size).ToList();
            var items = pageRows
                .Select(row => new PositionListItemDto(
                    row.Id,
                    row.Name,
                    row.Description,
                    row.CreatedAt))
                .ToList();

            var nextCursor = hasNextPage
                ? CreateCursor(pageRows[^1], query.DepartmentId)
                : null;

            _logger.LogInformation(
                "Positions loaded from database. ItemCount {ItemCount}. HasNextPage {HasNextPage}",
                items.Count,
                hasNextPage);

            return new PositionCursorPageDto(items, nextCursor);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to load positions from database");
            return DatabaseErrors.OperationFailed("load positions");
        }
    }

    private static Result<PositionCursor?, Error> ParseCursor(string? cursor, Guid? departmentId)
    {
        if (string.IsNullOrEmpty(cursor))
            return (PositionCursor?)null;

        try
        {
            var base64 = cursor.Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight((base64.Length + 3) / 4 * 4, '=');
            var parts = Encoding.UTF8.GetString(Convert.FromBase64String(base64)).Split(':');

            if (parts.Length != 4 || parts[0] != "v1" ||
                !long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks) ||
                ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks ||
                !Guid.TryParseExact(parts[2], "N", out var id) || id == Guid.Empty ||
                parts[3] != (departmentId?.ToString("N") ?? "-"))
            {
                return InvalidCursor();
            }

            return new PositionCursor(new DateTime(ticks, DateTimeKind.Utc), id);
        }
        catch (FormatException)
        {
            return InvalidCursor();
        }
    }

    private static string CreateCursor(PositionRow row, Guid? departmentId)
    {
        var value = $"v1:{row.CreatedAt.Ticks}:{row.Id:N}:{departmentId?.ToString("N") ?? "-"}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static Error InvalidCursor() =>
        Error.Validation("positions.cursor.invalid", "Position cursor is invalid.", "Cursor");

    private sealed record PositionCursor(DateTime CreatedAt, Guid Id);

    private sealed class PositionRow
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
