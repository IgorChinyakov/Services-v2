using CSharpFunctionalExtensions;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.ValueObjects.Department;
using Identifier = DirectoryService.Domain.ValueObjects.Department.Identifier;
using Name = DirectoryService.Domain.ValueObjects.Position.Name;

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

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<Department> Children => _children;

    public IReadOnlyList<DepartmentPosition> DepartmentPositions => _departmentPositions;

    public IReadOnlyList<DepartmentLocation> DepartmentLocations => _departmentLocations;

    public Department(Name name, Identifier identifier)
    {
        Name = name;
        Identifier = identifier;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public short Depth => (short)(Parent is null ? 0 : Parent.Depth + 1);

    public string Path => CreatePath();

    private string CreatePath()
    {
        var ids = new List<string>();
        var current = this;

        while (current is not null)
        {
            ids.Add(current.Identifier.Value.ToLowerInvariant());
            current = current.Parent;
        }

        ids.Reverse();

        return string.Join(".", ids);
    }
}