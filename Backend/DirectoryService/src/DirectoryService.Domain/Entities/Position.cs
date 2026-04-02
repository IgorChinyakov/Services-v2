using CSharpFunctionalExtensions;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.ValueObjects.Position;

namespace DirectoryService.Domain.Entities;

public class Position : Entity<PositionId>
{
    // ef core
    private Position()
    {
    }

    private readonly List<DepartmentPosition> _departmentPositions = [];

    public Name Name { get; private set; } = null!;

    public Description? Description { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<DepartmentPosition> DepartmentPositions => _departmentPositions;

    public Position(Name name, Description? description)
    {
        Name = name;
        Description = description;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}