using CSharpFunctionalExtensions;

namespace DirectoryService.Domain.Entities.Ids;

public class PositionId : ComparableValueObject
{
    public Guid Value { get; }

    private PositionId(Guid value)
    {
        Value = value;
    }

    public static PositionId Empty => new PositionId(Guid.Empty);

    public static PositionId New() => new PositionId(Guid.NewGuid());

    public static PositionId Create(Guid value) => new PositionId(value);

    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return Value;
    }
}