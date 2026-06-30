using DirectoryService.Domain.Entities.Ids;

namespace DirectoryService.Domain.Entities;

public class DepartmentPosition
{
    public DepartmentPosition(DepartmentId departmentId, PositionId positionId)
    {
        Id = Guid.NewGuid();
        DepartmentId = departmentId;
        PositionId = positionId;
    }

    // ef core
    private DepartmentPosition()
    {
    }

    public Guid Id { get; private set; }

    public Department Department { get; private set; } = null!;

    public DepartmentId DepartmentId { get; private set; } = null!;

    public Position Position { get; private set; } = null!;

    public PositionId PositionId { get; private set; } = null!;
}
