using CSharpFunctionalExtensions;
using DirectoryService.Domain.Shared;

namespace DirectoryService.Domain.ValueObjects.Location;

public class Name : ValueObject
{
    public string Value { get; }

    public const int MIN_LENGTH = 3;
    public const int MAX_LENGTH = 120;

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
            return Error.Validation("Location name is empty", "Location name");

        var trimmed = value.Trim();
        if (trimmed.Length < MIN_LENGTH || trimmed.Length > MAX_LENGTH)
            Error.Validation("Location name has invalid length", "Location name");

        return UnitResult.Success<Error>();
    }

    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}