using CSharpFunctionalExtensions;
using DirectoryService.Domain.Shared;

namespace DirectoryService.Domain.ValueObjects.Location;

public class LocationTimeZone : ValueObject
{
    public string Value { get; }

    private LocationTimeZone(string value)
    {
        Value = value;
    }

    public static Result<LocationTimeZone, Error> Create(string value)
    {
        var validationResult = Validate(value);
        if (validationResult.IsFailure)
            return validationResult.Error;

        return new LocationTimeZone(value);
    }

    public static UnitResult<Error> Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Error.Validation("TimeZone is empty", "TimeZone");

        var trimmed = value.Trim();
        if (!TimeZoneInfo.TryConvertWindowsIdToIanaId(trimmed, out string? ianaId))
            return Error.Validation("TimeZone has invalid IANA code", "TimeZone");

        return UnitResult.Success<Error>();
    }

    protected override IEnumerable<object> GetEqualityComponents() => throw new NotImplementedException();
}