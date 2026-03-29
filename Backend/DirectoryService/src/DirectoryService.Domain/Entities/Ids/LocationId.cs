using CSharpFunctionalExtensions;

namespace DirectoryService.Domain.Entities.Ids;

public class LocationId : ComparableValueObject
{
    public Guid Value { get; }

    private LocationId(Guid value)
    {
        Value = value;
    }

    public static LocationId Empty => new LocationId(Guid.Empty);

    public static LocationId New() => new LocationId(Guid.NewGuid());

    public static LocationId Create(Guid value) => new LocationId(value);

    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return Value;
    }
}