using DirectoryService.Domain.Entities.Ids;

namespace DirectoryService.Domain.Entities;

public class DepartmentLocation
{
    public DepartmentLocation(DepartmentId departmentId, LocationId locationId)
    {
        Id = Guid.NewGuid();
        DepartmentId = departmentId;
        LocationId = locationId;
    }

    // ef core
    private DepartmentLocation()
    {
    }

    public Guid Id { get; private set; }

    public Department Department { get; private set; } = null!;

    public DepartmentId DepartmentId { get; private set; } = null!;

    public Location Location { get; private set; } = null!;

    public LocationId LocationId { get; private set; } = null!;
}
