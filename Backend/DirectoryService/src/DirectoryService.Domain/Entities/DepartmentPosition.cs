using DirectoryService.Domain.Entities.Ids;

namespace DirectoryService.Domain.Entities;

public class DepartmentPosition
{
    public Guid Id { get; private set; }

    public Department Department { get; private set; }

    public DepartmentId DepartmentId { get; private set; }

    public Position Location { get; private set; }

    public PositionId LocationId { get; private set; }
}