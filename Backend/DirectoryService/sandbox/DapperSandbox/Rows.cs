#pragma warning disable SA1400, SA1402, SA1649

sealed class DepartmentRow
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Identifier { get; set; } = string.Empty;

    public Guid? ParentId { get; set; }

    public string Path { get; set; } = string.Empty;

    public int Depth { get; set; }

    public bool IsActive { get; set; }
}

sealed class LocationRow
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Timezone { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}

sealed class DepartmentLocationRow
{
    public string DepartmentName { get; set; } = string.Empty;

    public string DepartmentIdentifier { get; set; } = string.Empty;

    public string LocationName { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;
}

sealed class DepartmentLocationCountRow
{
    public string DepartmentIdentifier { get; set; } = string.Empty;

    public int LocationCount { get; set; }
}

sealed record DepartmentWithLocations(
    Guid Id,
    string Name,
    string Identifier,
    List<LocationRow> Locations);

sealed class PositionRow
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; }
}

sealed class DepartmentOptionalLocationRow
{
    public Guid DepartmentId { get; set; }

    public string DepartmentName { get; set; } = string.Empty;

    public string DepartmentIdentifier { get; set; } = string.Empty;

    public Guid? LocationId { get; set; }

    public string? LocationName { get; set; }
}

sealed record DepartmentWithPositions(
    Guid Id,
    string Name,
    string Identifier,
    List<PositionRow> Positions);

sealed record DepartmentCard(
    Guid Id,
    string Name,
    string Identifier,
    List<LocationRow> Locations,
    List<PositionRow> Positions);

sealed class DepartmentLocationNamesRow
{
    public string DepartmentIdentifier { get; set; } = string.Empty;

    public string[] LocationNames { get; set; } = [];
}

sealed class LocationDepartmentFlatRow
{
    public Guid LocationId { get; set; }

    public string LocationName { get; set; } = string.Empty;

    public Guid DepartmentId { get; set; }

    public string DepartmentName { get; set; } = string.Empty;

    public string DepartmentIdentifier { get; set; } = string.Empty;
}

sealed record DepartmentShortRow(
    Guid Id,
    string Name,
    string Identifier);

sealed record LocationWithDepartments(
    Guid Id,
    string Name,
    List<DepartmentShortRow> Departments);
