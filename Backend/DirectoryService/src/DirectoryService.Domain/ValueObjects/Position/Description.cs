using CSharpFunctionalExtensions;
using DirectoryService.Domain.Shared;

namespace DirectoryService.Domain.ValueObjects.Position;

public class Description : ValueObject
{
    public string Value { get; }

    public const int MAX_LENGTH = 1000;

    private Description(string value)
    {
        Value = value;
    }

    public static Result<Description, Error> Create(string value)
    {
        var validationResult = Validate(value);
        if (validationResult.IsFailure)
            return validationResult.Error;

        return new Description(value);
    }

    public static UnitResult<Error> Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return GeneralErrors.Validation("Position description is empty", nameof(Description));

        var trimmed = value.Trim();
        if (trimmed.Length > MAX_LENGTH)
            return GeneralErrors.Validation("Position description has invalid length", nameof(Description));

        return UnitResult.Success<Error>();
    }

    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}
