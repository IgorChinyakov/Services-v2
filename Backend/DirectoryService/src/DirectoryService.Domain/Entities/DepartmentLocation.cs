using DirectoryService.Domain.Entities.Ids;

namespace DirectoryService.Domain.Entities;

public class DepartmentLocation
{
    public Guid Id { get; private set; }

    public Department Department { get; private set; }

    public DepartmentId DepartmentId { get; private set; }

    public Location Location { get; private set; }

    public LocationId LocationId { get; private set; }
}