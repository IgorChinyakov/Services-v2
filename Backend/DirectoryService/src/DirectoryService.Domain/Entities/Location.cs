using CSharpFunctionalExtensions;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.ValueObjects.Location;

namespace DirectoryService.Domain.Entities;

public class Location : Entity<LocationId>
{
    // ef core
    private Location()
    {
    }

    private readonly List<DepartmentLocation> _departmentLocations = [];

    public Name Name { get; private set; } = null!;

    public Address Address { get; private set; } = null!;

    public LocationTimeZone TimeZone { get; private set; } = null!;

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<DepartmentLocation> DepartmentLocations => _departmentLocations;

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