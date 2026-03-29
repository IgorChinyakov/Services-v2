using CSharpFunctionalExtensions;
using DirectoryService.Domain.Shared;

namespace DirectoryService.Domain.ValueObjects.Department;

public class Name : ValueObject
{
    public string Value { get; }

    public const int MIN_LENGTH = 3;
    public const int MAX_LENGTH = 150;

    private Name(string value)
    {
        Value = value;
    }

    public static Result<Name, Error> Create(string value)
    {
        var validationResult = Validate(value);
        if (validationResult.IsFailure)
            return validationResult.Error;

        return new Name(value);
    }

    public static UnitResult<Error> Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Error.Validation("Department name is empty", "Department name");

        var trimmed = value.Trim();
        if (trimmed.Length < MIN_LENGTH || trimmed.Length > MAX_LENGTH)
            Error.Validation("Department name has invalid length", "Department name");

        return UnitResult.Success<Error>();
    }

    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}