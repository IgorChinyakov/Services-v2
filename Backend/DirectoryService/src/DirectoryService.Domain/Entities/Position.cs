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

    public Position(
        Name name,
        Description? description,
        IEnumerable<DepartmentId> departmentIds)
    {
        Id = PositionId.New();
        Name = name;
        Description = description;
        IsActive = true;

        var utcNow = DateTime.UtcNow;
        CreatedAt = utcNow;
        UpdatedAt = utcNow;

        foreach (var departmentId in departmentIds.Distinct())
            AddDepartment(departmentId);
    }

    public void AddDepartment(DepartmentId departmentId)
    {
        _departmentPositions.Add(new DepartmentPosition(departmentId, Id));
    }
}
