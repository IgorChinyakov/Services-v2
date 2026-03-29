using CSharpFunctionalExtensions;

namespace DirectoryService.Domain.Entities.Ids;

public class DepartmentId : ComparableValueObject
{
    public Guid Value { get; }

    private DepartmentId(Guid value)
    {
        Value = value;
    }

    public static DepartmentId Empty => new DepartmentId(Guid.Empty);

    public static DepartmentId New() => new DepartmentId(Guid.NewGuid());

    public static DepartmentId Create(Guid value) => new DepartmentId(value);

    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return Value;
    }
}