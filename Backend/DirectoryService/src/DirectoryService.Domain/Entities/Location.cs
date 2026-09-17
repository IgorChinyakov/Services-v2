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

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<DepartmentLocation> DepartmentLocations => _departmentLocations;

    public Location(Name name, Address address)
    {
        Id = LocationId.New();
        Name = name;
        Address = address;
        IsActive = true;

        var utcNow = DateTime.UtcNow;

        CreatedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public void Update(Name name, Address address)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(address);

        Name = name;
        Address = address;
        UpdatedAt = DateTime.UtcNow;
    }
}
