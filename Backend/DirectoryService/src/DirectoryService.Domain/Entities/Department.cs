using CSharpFunctionalExtensions;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.ValueObjects.Department;

namespace DirectoryService.Domain.Entities;

public class Department : Entity<DepartmentId>
{
    // ef core
    private Department()
    {
    }

    private readonly List<Department> _children = [];
    private readonly List<DepartmentPosition> _departmentPositions = [];
    private readonly List<DepartmentLocation> _departmentLocations = [];

    public Name Name { get; private set; } = null!;

    public Identifier Identifier { get; private set; } = null!;

    public Department? Parent { get; private set; }

    public DepartmentId? ParentId { get; private set; }

    public short Depth { get; private set; }

    public string Path { get; private set; } = null!;

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<Department> Children => _children;

    public IReadOnlyList<DepartmentPosition> DepartmentPositions => _departmentPositions;

    public IReadOnlyList<DepartmentLocation> DepartmentLocations => _departmentLocations;

    public Department(
        Name name,
        Identifier identifier,
        Department? parent,
        IEnumerable<LocationId> locationIds)
    {
        Id = DepartmentId.New();
        Name = name;
        Identifier = identifier;
        Parent = parent;
        ParentId = parent?.Id;
        Depth = parent is null ? (short)0 : checked((short)(parent.Depth + 1));
        Path = parent is null
            ? identifier.Value.ToLowerInvariant()
            : $"{parent.Path}.{identifier.Value.ToLowerInvariant()}";
        IsActive = true;

        var utcNow = DateTime.UtcNow;
        CreatedAt = utcNow;
        UpdatedAt = utcNow;

        foreach (var locationId in locationIds.Distinct())
            AddLocation(locationId);
    }

    public void AddLocation(LocationId locationId)
    {
        _departmentLocations.Add(new DepartmentLocation(Id, locationId));
    }
}
