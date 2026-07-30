#pragma warning disable CS0219, SA1028, SA1400, SA1402, SA1513, SA1515, SA1518, SA1519, SA1649

using System.Data;
using System.Text;
using Dapper;
using Npgsql;

const string DefaultConnectionString =
    "Host=localhost;Port=5438;Database=postgres;Username=postgres;Password=postgres";

var connectionString = Environment.GetEnvironmentVariable("DAPPER_PLAYGROUND_CONNECTION")
                       ?? DefaultConnectionString;

Console.WriteLine("Dapper playground");
Console.WriteLine("Run: dotnet run --project sandbox/DapperSandbox/DapperSandbox.csproj");
Console.WriteLine("Connection env: DAPPER_PLAYGROUND_CONNECTION");
Console.WriteLine();

await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();

PrintSeparator("00 Example scalar query");

static Task<int> CountAllDepartmentsExampleAsync(IDbConnection connection)
{
    const string sql = """
                       select count(*)
                       from dapper_playground.departments
                       """;

    return connection.ExecuteScalarAsync<int>(sql);
}

await RunDbAsync("00 CountAllDepartmentsExampleAsync", async () =>
{
    var count = await CountAllDepartmentsExampleAsync(connection);

    Expect(5, count);
});

PrintSeparator("01 ExecuteScalarAsync with where");

static Task<int> CountActiveDepartmentsAsync(IDbConnection connection)
{
    // TODO 01: return active departments count.
    const string sql = """
                       select count(*)
                       from dapper_playground.departments
                       where is_active = true
                       """;

    return connection.ExecuteScalarAsync<int>(sql);
}

await RunDbAsync("01 CountActiveDepartmentsAsync", async () =>
{
    var count = await CountActiveDepartmentsAsync(connection);

    Expect(4, count);
});

PrintSeparator("02 QuerySingleAsync mapped row");

static Task<DepartmentRow> GetDepartmentByIdentifierAsync(
    IDbConnection connection,
    string identifier)
{
    // TODO 02: find department by identifier.
    // Use QuerySingleAsync<DepartmentRow>.
    // Alias snake_case columns to PascalCase properties: parent_id as ParentId.
    const string sql = """
                       select 
                           id as Id, 
                           name as Name, 
                           identifier as Identifier, 
                           parent_id as ParentId, 
                           path as Path, 
                           depth as Depth, 
                           is_active as IsActive
                       from dapper_playground.departments
                       where Identifier = @Identifier
                       """;

    var result = connection.QuerySingleAsync<DepartmentRow>(sql, new { Identifier = identifier });

    return result;
}

await RunDbAsync("02 GetDepartmentByIdentifierAsync", async () =>
{
    var department = await GetDepartmentByIdentifierAsync(connection, "it");

    Expect("IT Department", department.Name);
    Expect("hq.it", department.Path);
    Expect(1, department.Depth);
});

PrintSeparator("03 QueryAsync list with parameters");

static Task<IEnumerable<DepartmentRow>> GetChildrenAsync(
    IDbConnection connection,
    Guid parentId)
{
    // TODO 03: return active child departments by parent_id ordered by identifier.
    // Use QueryAsync<DepartmentRow> and an anonymous object parameter.
    const string sql = """
                       select 
                            id as Id, 
                            name as Name, 
                            identifier as Identifier, 
                            parent_id as ParentId, 
                            path as Path, 
                            depth as Depth, 
                            is_active as IsActive
                       from dapper_playground.departments
                       where parent_id = @ParentId
                       order by identifier
                       """;

    var result = connection.QueryAsync<DepartmentRow>(sql, new { ParentId = parentId });

    return result;
}

await RunDbAsync("03 GetChildrenAsync", async () =>
{
    var hq = await GetSeedDepartmentAsync(connection, "hq");
    var children = await GetChildrenAsync(connection, hq.Id);

    ExpectSequence(["it", "sales"], children.Select(x => x.Identifier));
});

PrintSeparator("04 ExecuteAsync insert");

static Task<int> InsertLocationAsync(
    IDbConnection connection,
    Guid id,
    string name,
    string city,
    string timezone)
{
    // TODO 04: insert location and return affected rows count.
    // Use ExecuteAsync with parameters.
    const string sql = """
                       insert into dapper_playground.locations (id, name, city, timezone, is_active)
                       values (@Id, @Name, @City, @Timezone, true)
                       """;

    var result = connection.ExecuteAsync(sql, new { Id = id, Name = name, City = city, Timezone = timezone });
    return result;
}

await RunDbAsync("04 InsertLocationAsync", async () =>
{
    var locationId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    var affectedRows = await InsertLocationAsync(
        connection,
        locationId,
        "Kazan Office",
        "Kazan",
        "Europe/Moscow");

    var created = await connection.QuerySingleAsync<string>(
        """
        select city
        from dapper_playground.locations
        where id = @LocationId;
        """,
        new { LocationId = locationId });

    Expect(1, affectedRows);
    Expect("Kazan", created);
});

PrintSeparator("05 ExecuteAsync update");

static Task<int> DeactivateDepartmentAsync(
    IDbConnection connection,
    string identifier)
{
    // TODO 05: set is_active = false by identifier and return affected rows count.
    const string sql = """
                       update dapper_playground.departments 
                       set is_active = false
                       where identifier = @Identifier
                       """;

    var result = connection.ExecuteAsync(sql, new { Identifier = identifier });

    return result;
}

await RunDbAsync("05 DeactivateDepartmentAsync", async () =>
{
    var affectedRows = await DeactivateDepartmentAsync(connection, "dev");

    var isActive = await connection.QuerySingleAsync<bool>(
        """
        select is_active
        from dapper_playground.departments
        where identifier = @Identifier;
        """,
        new { Identifier = "dev" });

    Expect(1, affectedRows);
    Expect(false, isActive);
});

PrintSeparator("06 QueryFirstOrDefaultAsync missing row");

static Task<DepartmentRow?> GetMaybeDepartmentAsync(
    IDbConnection connection,
    string identifier)
{
    // TODO 06: return department or null if it does not exist.
    // Use QueryFirstOrDefaultAsync<DepartmentRow>.
    const string sql = """
                       select 
                            id as Id, 
                            name as Name, 
                            identifier as Identifier, 
                            parent_id as ParentId, 
                            path as Path, 
                            depth as Depth, 
                            is_active as IsActive
                       from dapper_playground.departments
                       where identifier = @Identifier
                       """;

    return connection.QueryFirstOrDefaultAsync<DepartmentRow>(sql, new { Identifier = identifier });
}

await RunDbAsync("06 GetMaybeDepartmentAsync", async () =>
{
    var existing = await GetMaybeDepartmentAsync(connection, "sales");
    var missing = await GetMaybeDepartmentAsync(connection, "missing");

    Expect("Sales Department", existing?.Name);
    Expect(null, missing);
});

PrintSeparator("07 Array parameter with any()");

static Task<IEnumerable<string>> GetDepartmentNamesByIdentifiersAsync(
    IDbConnection connection,
    string[] identifiers)
{
    // TODO 07: return names by identifiers, ordered by name.
    // PostgreSQL hint: where identifier = any(@Identifiers)
    const string sql = """
                       select name
                       from dapper_playground.departments
                       where identifier = any(@Identifiers)
                       order by name
                       """;

    return connection.QueryAsync<string>(sql, new { Identifiers = identifiers });
}

await RunDbAsync("07 GetDepartmentNamesByIdentifiersAsync", async () =>
{
    var names = await GetDepartmentNamesByIdentifiersAsync(connection, ["dev", "hq", "missing"]);

    ExpectSequence(["Development Team", "Head Office"], names);
});

PrintSeparator("08 Join query");

static Task<IEnumerable<DepartmentLocationRow>> GetDepartmentLocationsAsync(
    IDbConnection connection,
    string departmentIdentifier)
{
    // TODO 08: return locations attached to department by department identifier.
    // Join departments, department_locations, locations.
    const string sql = """
                       select 
                            d.name as DepartmentName, 
                            d.identifier as DepartmentIdentifier, 
                            l.name as LocationName, 
                            l.city as City
                       from dapper_playground.departments as d
                       join dapper_playground.department_locations as dl on d.id = dl.department_id
                       join dapper_playground.locations as l on l.id = dl.location_id
                       where d.identifier = @DepartmentIdentifier
                       """;

    return connection.QueryAsync<DepartmentLocationRow>(sql, new { DepartmentIdentifier = departmentIdentifier });
}

await RunDbAsync("08 GetDepartmentLocationsAsync", async () =>
{
    var locations = await GetDepartmentLocationsAsync(connection, "it");

    ExpectSequence(["Saint Petersburg Office"], locations.Select(x => x.LocationName));
});

PrintSeparator("09 Group by aggregate");

static Task<IEnumerable<DepartmentLocationCountRow>> GetLocationCountsByDepartmentAsync(
    IDbConnection connection)
{
    // TODO 09: return active departments and count of their locations.
    // Use left join, group by, order by DepartmentIdentifier.
    const string sql = """
                       select
                           d.identifier as DepartmentIdentifier,
                           count(l.id) as LocationCount
                       from dapper_playground.departments as d
                                left join dapper_playground.department_locations as dl on d.id = dl.department_id
                                join dapper_playground.locations as l on l.id = dl.location_id
                       group by d.identifier
                       order by d.identifier
                       """;

    return connection.QueryAsync<DepartmentLocationCountRow>(sql);
}

await RunDbAsync("09 GetLocationCountsByDepartmentAsync", async () =>
{
    var rows = await GetLocationCountsByDepartmentAsync(connection);

    ExpectSequence(
        ["dev:1", "hq:1", "it:1", "sales:1"],
        rows.Select(x => $"{x.DepartmentIdentifier}:{x.LocationCount}"));
});

PrintSeparator("10 DynamicParameters");

static Task<IEnumerable<LocationRow>> SearchLocationsAsync(
    IDbConnection connection,
    string? city,
    bool onlyActive)
{
    // TODO 10: use DynamicParameters.
    // If city is not null, filter by city.
    // If onlyActive is true, filter by is_active = true.
    // Order by name.
    var parameters = new DynamicParameters();

    StringBuilder sql = new StringBuilder("""
                                          select
                                              l.id as Id,
                                              l.name as Name,
                                              l.city as City,
                                              l.timezone as Timezone,
                                              l.is_active as IsActive
                                          from dapper_playground.locations as l
                                          where 1 = 1
                                          """);

    sql.AppendLine();

    if (city is not null)
    {
        sql.AppendLine("and city = @City");
        parameters.Add("City", city);
    }

    if (onlyActive)
    {
        sql.AppendLine("and is_active = true");
    }

    sql.AppendLine("order by name");

    return connection.QueryAsync<LocationRow>(sql.ToString(), parameters);
}

await RunDbAsync("10 SearchLocationsAsync", async () =>
{
    var locations = await SearchLocationsAsync(connection, "Moscow", onlyActive: true);

    ExpectSequence(["Moscow Office"], locations.Select(x => x.Name));
});

PrintSeparator("11 Pagination");

static Task<IEnumerable<DepartmentRow>> GetDepartmentsPageAsync(
    IDbConnection connection,
    int limit,
    int offset)
{
    // TODO 11: return active departments ordered by identifier with limit/offset.
    const string sql = """
                       select 
                            id as Id, 
                            name as Name, 
                            identifier as Identifier, 
                            parent_id as ParentId, 
                            path as Path, 
                            depth as Depth, 
                            is_active as IsActive
                       from dapper_playground.departments
                       where is_active = true
                       order by identifier
                       limit @Limit
                       offset @Offset
                       """;

    return connection.QueryAsync<DepartmentRow>(sql, new { Limit = limit, Offset = offset });
}

await RunDbAsync("11 GetDepartmentsPageAsync", async () =>
{
    var page = await GetDepartmentsPageAsync(connection, limit: 2, offset: 1);

    ExpectSequence(["hq", "it"], page.Select(x => x.Identifier));
});

PrintSeparator("12 Transaction commit");

static async Task<Guid> CreatePositionInTransactionAsync(
    NpgsqlConnection connection,
    string name,
    string description)
{
    // TODO 12: begin transaction, insert position, commit, return id.
    // Pass transaction into ExecuteAsync.
    await using var transaction = await connection.BeginTransactionAsync();
    const string sql = """
                       insert into dapper_playground.positions (id, name, description, is_active)
                       values (@Id, @Name, @Description, true)
                       """;

    var id = Guid.NewGuid();

    var result =
        await connection.ExecuteAsync(sql, new { Id = id, Name = name, Description = description, }, transaction);

    await transaction.CommitAsync();

    return id;
}

await RunDbAsync("12 CreatePositionInTransactionAsync", async () =>
{
    var positionId = await CreatePositionInTransactionAsync(
        connection,
        "Support Engineer",
        "Helps customers");

    var name = await connection.QuerySingleAsync<string>(
        """
        select name
        from dapper_playground.positions
        where id = @PositionId;
        """,
        new { PositionId = positionId });

    Expect("Support Engineer", name);
});

PrintSeparator("13 Transaction rollback");

static async Task<Guid> TryCreatePositionAndRollbackAsync(
    NpgsqlConnection connection,
    string name)
{
    // TODO 13: begin transaction, insert position, then rollback.
    // Return generated position id.
    // After this method position must not exist.
    await using var transaction = await connection.BeginTransactionAsync();
    const string sql = """
                       insert into dapper_playground.positions (id, name, is_active)
                       values (@Id, @Name, true)
                       """;

    var id = Guid.NewGuid();

    var result = await connection.ExecuteAsync(sql, new { Id = id, Name = name, }, transaction);

    await transaction.RollbackAsync();

    return id;
}

await RunDbAsync("13 TryCreatePositionAndRollbackAsync", async () =>
{
    var positionId = await TryCreatePositionAndRollbackAsync(connection, "Temporary Position");

    var count = await connection.QuerySingleAsync<int>(
        """
        select count(*)
        from dapper_playground.positions
        where id = @PositionId;
        """,
        new { PositionId = positionId });

    Expect(false, positionId == Guid.Empty);
    Expect(0, count);
});

PrintSeparator("14 Multi-mapping one-to-many");

static async Task<DepartmentWithLocations> GetDepartmentWithLocationsAsync(
    IDbConnection connection,
    string identifier)
{
    // TODO 14: use Dapper multi-mapping:
    // QueryAsync<DepartmentRow, LocationRow, DepartmentWithLocations>(..., splitOn: "Id")
    // Return one DepartmentWithLocations with all locations.
    var lookup = new Dictionary<Guid, DepartmentWithLocations>();

    const string sql = """
                       select 
                            d.id as Id,
                            d.name as Name,
                            d.identifier as Identifier,
                            d.parent_id as ParentId,
                            d.path as Path,
                            d.depth as Depth,
                            d.is_active as IsActive,
                            l.id as Id, 
                            l.name as Name,
                            l.city as City,
                            l.timezone as Timezone,
                            l.is_active as IsActive
                       from dapper_playground.departments as d
                       join dapper_playground.department_locations as dl on d.id = dl.department_id
                       join dapper_playground.locations as l on l.id = dl.location_id
                       where d.Identifier = @Identifier
                       """;

    var result = await connection.QueryAsync<
        DepartmentRow,
        LocationRow,
        DepartmentWithLocations>(
        sql,
        map: (department, location) =>
        {
            if (!lookup.TryGetValue(department.Id, out var result))
            {
                result = new DepartmentWithLocations(
                    department.Id,
                    department.Name,
                    department.Identifier,
                    []);

                lookup.Add(result.Id, result);
            }

            result.Locations.Add(location);

            return result;
        },
        param: new { Identifier = identifier, },
        splitOn: "Id");

    return lookup.Values.Single();
}

await RunDbAsync("14 GetDepartmentWithLocationsAsync", async () =>
{
    var department = await GetDepartmentWithLocationsAsync(connection, "sales");

    Expect("Sales Department", department.Name);
    ExpectSequence(["Moscow Office"], department.Locations.Select(x => x.Name));
});

PrintSeparator("15 Multi-mapping all departments with locations");

static async Task<IReadOnlyList<DepartmentWithLocations>> GetActiveDepartmentsWithLocationsAsync(
    IDbConnection connection)
{
    // TODO 15: return all active departments with their locations.
    // Use Dapper multi-mapping + Dictionary<Guid, DepartmentWithLocations>.
    // Order departments by identifier and locations by name.
    var lookup = new Dictionary<Guid, DepartmentWithLocations>();

    const string sql = """
                       select 
                            d.id as Id,
                            d.name as Name,
                            d.identifier as Identifier,
                            d.parent_id as ParentId,
                            d.path as Path,
                            d.depth as Depth,
                            d.is_active as IsActive,
                            l.id as Id, 
                            l.name as Name,
                            l.city as City,
                            l.timezone as Timezone,
                            l.is_active as IsActive
                       from dapper_playground.departments d
                       join dapper_playground.department_locations dl on dl.department_id = d.id
                       join dapper_playground.locations l on l.id = dl.location_id
                       where d.is_active = true
                       order by d.identifier, l.name
                       """;

    var queryResult = await connection.QueryAsync<
        DepartmentRow, 
        LocationRow, 
        DepartmentWithLocations>(
        sql,
        map: (departmentRow, locationRow) =>
        {
            if (!lookup.TryGetValue(departmentRow.Id, out var result))
            {
                result = new DepartmentWithLocations(
                    departmentRow.Id, 
                    departmentRow.Name, 
                    departmentRow.Identifier,
                    []);
                
                lookup.Add(departmentRow.Id, result);
            }
            
            result.Locations.Add(locationRow);

            return result;
        }, 
        splitOn: "Id");

    return lookup.Values.ToList();
}

await RunDbAsync("15 GetActiveDepartmentsWithLocationsAsync", async () =>
{
    var departments = await GetActiveDepartmentsWithLocationsAsync(connection);

    ExpectSequence(
        ["dev:Remote", "hq:Moscow Office", "it:Saint Petersburg Office", "sales:Moscow Office"],
        departments.Select(x => $"{x.Identifier}:{string.Join(", ", x.Locations.Select(l => l.Name))}"));
});

PrintSeparator("16 Left join with optional location");

static Task<IEnumerable<DepartmentOptionalLocationRow>> GetDepartmentsWithOptionalLocationRowsAsync(
    IDbConnection connection)
{
    // TODO 16: return active departments with optional location using left join.
    // Include departments even if location is missing.
    // Use DepartmentOptionalLocationRow and order by DepartmentIdentifier, LocationName.
    return Task.FromResult(Enumerable.Empty<DepartmentOptionalLocationRow>());
}

await RunDbAsync("16 GetDepartmentsWithOptionalLocationRowsAsync", async () =>
{
    await connection.ExecuteAsync(
        """
        insert into dapper_playground.departments
            (id, name, identifier, parent_id, path, depth, is_active)
        values
            ('10000000-0000-0000-0000-000000000006', 'Legal Department', 'legal', null, 'legal', 0, true);
        """);

    var rows = await GetDepartmentsWithOptionalLocationRowsAsync(connection);

    ExpectSequence(
        ["dev:Remote", "hq:Moscow Office", "it:Saint Petersburg Office", "legal:<null>", "sales:Moscow Office"],
        rows.Select(x => $"{x.DepartmentIdentifier}:{x.LocationName ?? "<null>"}"));
});

PrintSeparator("17 Department with positions");

static async Task<DepartmentWithPositions> GetDepartmentWithPositionsAsync(
    IDbConnection connection,
    string identifier)
{
    // TODO 17: return one department with positions.
    // Use departments -> department_positions -> positions.
    // Use multi-mapping + dictionary, similar to task 14.
    await Task.CompletedTask;

    return new DepartmentWithPositions(Guid.Empty, string.Empty, string.Empty, []);
}

await RunDbAsync("17 GetDepartmentWithPositionsAsync", async () =>
{
    var department = await GetDepartmentWithPositionsAsync(connection, "it");

    Expect("IT Department", department.Name);
    ExpectSequence(["Developer"], department.Positions.Select(x => x.Name));
});

PrintSeparator("18 Department card with two collections");

static async Task<DepartmentCard> GetDepartmentCardAsync(
    IDbConnection connection,
    string identifier)
{
    // TODO 18: return one read model with department, locations and positions.
    // You can solve it with 3 separate Dapper queries:
    // 1. department by identifier
    // 2. locations by department id
    // 3. positions by department id
    await Task.CompletedTask;

    return new DepartmentCard(Guid.Empty, string.Empty, string.Empty, [], []);
}

await RunDbAsync("18 GetDepartmentCardAsync", async () =>
{
    var card = await GetDepartmentCardAsync(connection, "sales");

    Expect("Sales Department", card.Name);
    ExpectSequence(["Moscow Office"], card.Locations.Select(x => x.Name));
    ExpectSequence(["Manager"], card.Positions.Select(x => x.Name));
});

PrintSeparator("19 SQL array aggregation");

static Task<IEnumerable<DepartmentLocationNamesRow>> GetDepartmentLocationNamesAsync(
    IDbConnection connection)
{
    // TODO 19: return active departments with location names aggregated into string[].
    // PostgreSQL hint: array_agg(l.name order by l.name) filter (where l.id is not null)
    // For departments without locations return empty array, not null.
    return Task.FromResult(Enumerable.Empty<DepartmentLocationNamesRow>());
}

await RunDbAsync("19 GetDepartmentLocationNamesAsync", async () =>
{
    await connection.ExecuteAsync(
        """
        insert into dapper_playground.departments
            (id, name, identifier, parent_id, path, depth, is_active)
        values
            ('10000000-0000-0000-0000-000000000006', 'Legal Department', 'legal', null, 'legal', 0, true);
        """);

    var rows = await GetDepartmentLocationNamesAsync(connection);

    ExpectSequence(
        ["dev:Remote", "hq:Moscow Office", "it:Saint Petersburg Office", "legal:", "sales:Moscow Office"],
        rows.Select(x => $"{x.DepartmentIdentifier}:{string.Join(", ", x.LocationNames)}"));
});

PrintSeparator("20 Flat rows to nested read model");

static async Task<IReadOnlyList<LocationWithDepartments>> GetLocationsWithDepartmentsAsync(
    IDbConnection connection)
{
    // TODO 20: query flat rows and group them in C# into LocationWithDepartments.
    // This time do not use Dapper multi-mapping; use LocationDepartmentFlatRow + dictionary.
    await Task.CompletedTask;

    return [];
}

await RunDbAsync("20 GetLocationsWithDepartmentsAsync", async () =>
{
    var locations = await GetLocationsWithDepartmentsAsync(connection);

    ExpectSequence(
        [
            "Moscow Office:Head Office, Sales Department", "Remote:Development Team",
            "Saint Petersburg Office:IT Department"
        ],
        locations.Select(x => $"{x.Name}:{string.Join(", ", x.Departments.Select(d => d.Name))}"));
});

Console.WriteLine();
Console.WriteLine("Edit TODOs, run again, repeat. SQL is just code with a different hat.");

async Task RunDbAsync(string name, Func<Task> test)
{
    await PrepareDatabaseAsync(connection);
    await RunAsync(name, test);
}

static async Task PrepareDatabaseAsync(NpgsqlConnection connection)
{
    const string sql = """
                       drop schema if exists dapper_playground cascade;
                       create schema dapper_playground;

                       create table dapper_playground.departments
                       (
                           id uuid primary key,
                           name text not null,
                           identifier text not null unique,
                           parent_id uuid null references dapper_playground.departments(id),
                           path text not null,
                           depth integer not null,
                           is_active boolean not null
                       );

                       create table dapper_playground.locations
                       (
                           id uuid primary key,
                           name text not null unique,
                           city text not null,
                           timezone text not null,
                           is_active boolean not null
                       );

                       create table dapper_playground.department_locations
                       (
                           department_id uuid not null references dapper_playground.departments(id),
                           location_id uuid not null references dapper_playground.locations(id),
                           primary key (department_id, location_id)
                       );

                       create table dapper_playground.positions
                       (
                           id uuid primary key,
                           name text not null unique,
                           description text null,
                           is_active boolean not null
                       );

                       create table dapper_playground.department_positions
                       (
                           department_id uuid not null references dapper_playground.departments(id),
                           position_id uuid not null references dapper_playground.positions(id),
                           primary key (department_id, position_id)
                       );

                       insert into dapper_playground.departments
                           (id, name, identifier, parent_id, path, depth, is_active)
                       values
                           ('10000000-0000-0000-0000-000000000001', 'Head Office', 'hq', null, 'hq', 0, true),
                           ('10000000-0000-0000-0000-000000000002', 'Sales Department', 'sales', '10000000-0000-0000-0000-000000000001', 'hq.sales', 1, true),
                           ('10000000-0000-0000-0000-000000000003', 'IT Department', 'it', '10000000-0000-0000-0000-000000000001', 'hq.it', 1, true),
                           ('10000000-0000-0000-0000-000000000004', 'Development Team', 'dev', '10000000-0000-0000-0000-000000000003', 'hq.it.dev', 2, true),
                           ('10000000-0000-0000-0000-000000000005', 'Archive Department', 'archive', null, 'archive', 0, false);

                       insert into dapper_playground.locations
                           (id, name, city, timezone, is_active)
                       values
                           ('20000000-0000-0000-0000-000000000001', 'Moscow Office', 'Moscow', 'Europe/Moscow', true),
                           ('20000000-0000-0000-0000-000000000002', 'Saint Petersburg Office', 'Saint Petersburg', 'Europe/Moscow', true),
                           ('20000000-0000-0000-0000-000000000003', 'Remote', 'Remote', 'UTC', true),
                           ('20000000-0000-0000-0000-000000000004', 'Closed Warehouse', 'Tula', 'Europe/Moscow', false);

                       insert into dapper_playground.department_locations
                           (department_id, location_id)
                       values
                           ('10000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000001'),
                           ('10000000-0000-0000-0000-000000000002', '20000000-0000-0000-0000-000000000001'),
                           ('10000000-0000-0000-0000-000000000003', '20000000-0000-0000-0000-000000000002'),
                           ('10000000-0000-0000-0000-000000000004', '20000000-0000-0000-0000-000000000003');

                       insert into dapper_playground.positions
                           (id, name, description, is_active)
                       values
                           ('30000000-0000-0000-0000-000000000001', 'Manager', 'Manages processes', true),
                           ('30000000-0000-0000-0000-000000000002', 'Developer', 'Writes code', true),
                           ('30000000-0000-0000-0000-000000000003', 'Accountant', 'Works with money', true),
                           ('30000000-0000-0000-0000-000000000004', 'Old Role', 'Deprecated', false);

                       insert into dapper_playground.department_positions
                           (department_id, position_id)
                       values
                           ('10000000-0000-0000-0000-000000000002', '30000000-0000-0000-0000-000000000001'),
                           ('10000000-0000-0000-0000-000000000003', '30000000-0000-0000-0000-000000000002'),
                           ('10000000-0000-0000-0000-000000000004', '30000000-0000-0000-0000-000000000002');
                       """;

    await connection.ExecuteAsync(sql);
}

static Task<DepartmentRow> GetSeedDepartmentAsync(IDbConnection connection, string identifier)
{
    const string sql = """
                       select
                           id as Id,
                           name as Name,
                           identifier as Identifier,
                           parent_id as ParentId,
                           path as Path,
                           depth as Depth,
                           is_active as IsActive
                       from dapper_playground.departments
                       where identifier = @Identifier;
                       """;

    return connection.QuerySingleAsync<DepartmentRow>(sql, new { Identifier = identifier });
}

static void PrintSeparator(string title)
{
    Console.WriteLine();
    Console.WriteLine("========================================");
    Console.WriteLine(title);
    Console.WriteLine("----------------------------------------");
}

static async Task RunAsync(string name, Func<Task> test)
{
    try
    {
        await test();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception exception)
    {
        Console.WriteLine($"FAIL {name}: {exception.Message}");
    }
}

static void Expect<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"expected {expected}, actual {actual}");
    }
}

static void ExpectSequence<T>(IEnumerable<T> expected, IEnumerable<T> actual)
{
    var expectedArray = expected.ToArray();
    var actualArray = actual.ToArray();

    if (!expectedArray.SequenceEqual(actualArray))
    {
        throw new InvalidOperationException(
            $"expected [{Format(expectedArray)}], actual [{Format(actualArray)}]");
    }
}

static string Format<T>(IEnumerable<T> values)
{
    var builder = new StringBuilder();

    foreach (var value in values)
    {
        if (builder.Length > 0)
        {
            builder.Append(", ");
        }

        builder.Append(value);
    }

    return builder.ToString();
}
