using CSharpFunctionalExtensions;
using DirectoryService.Domain.Shared;

namespace DirectoryService.Domain.ValueObjects.Location;

public class Address : ValueObject
{
    public string Country { get; }

    public string City { get; }

    public string Street { get; }

    public string Building { get; }

    public const int MIN_PART_LENGTH = 2;
    public const int MAX_PART_LENGTH = 100;

    private Address(
        string country,
        string city,
        string street,
        string building)
    {
        Country = country;
        City = city;
        Street = street;
        Building = building;
    }

    public static Result<Address, Error> Create(
        string country,
        string city,
        string street,
        string building)
    {
        var validationResult = Validate(country, city, street, building);
        if (validationResult.IsFailure)
            return validationResult.Error;

        return new Address(
            country.Trim(),
            city.Trim(),
            street.Trim(),
            building.Trim());
    }

    public static UnitResult<Error> Validate(
        string country,
        string city,
        string street,
        string building)
    {
        var messages = new List<ErrorMessage>();

        ValidateField(country, nameof(Country), "Country", messages);
        ValidateField(city, nameof(City), "City", messages);
        ValidateField(street, nameof(Street), "Street", messages);
        ValidateField(building, nameof(Building), "Building", messages);

        return messages.Count > 0
            ? Error.Validation(messages)
            : UnitResult.Success<Error>();
    }

    public override string ToString()
    {
        var parts = new List<string>();

        parts.Add(Country);
        parts.Add(City);
        parts.Add(Street);
        parts.Add(Building);

        return string.Join(", ", parts);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Country;
        yield return City;
        yield return Street;
        yield return Building;
    }

    private static void ValidateField(
        string value,
        string propertyName,
        string displayName,
        List<ErrorMessage> messages)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            messages.Add(new($"{displayName} is empty", propertyName));
            return;
        }

        var trimmed = value.Trim();

        if (trimmed.Length < MIN_PART_LENGTH || trimmed.Length > MAX_PART_LENGTH)
            messages.Add(new($"{displayName} has invalid length", propertyName));
    }
}