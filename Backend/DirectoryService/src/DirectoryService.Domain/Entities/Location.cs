using DirectoryService.Domain.ValueObjects.Location;

namespace DirectoryService.Domain.Entities;

public class Location
{
    private readonly List<Department> _departments = [];

    public Name Name { get; private set; }

    public Address Address { get; private set; }

    public LocationTimeZone TimeZone { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<Department> Departments => _departments;

    public Location(Name name, Address address, LocationTimeZone timeZone)
    {
        Name = name;
        Address = address;
        TimeZone = timeZone;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}