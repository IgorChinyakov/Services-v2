using CSharpFunctionalExtensions;
using DirectoryService.Domain.Shared;

namespace DirectoryService.Domain.ValueObjects.Location;

public class Address : ValueObject
{
    public string City { get; }

    public string Street { get; }

    public string House { get; }

    public string Apartment { get; }

    public const int MIN_PART_LENGTH = 2;
    public const int MAX_PART_LENGTH = 100;

    private Address(
        string city,
        string street,
        string house,
        string apartment)
    {
        City = city;
        Street = street;
        House = house;
        Apartment = apartment;
    }

    public static Result<Address, Error> Create(
        string city,
        string street,
        string house,
        string apartment)
    {
        var validationResult = Validate(city, street, house, apartment);
        if (validationResult.IsFailure)
            return validationResult.Error;

        return new Address(
            city.Trim(),
            street.Trim(),
            house.Trim(),
            apartment.Trim());
    }

    public static UnitResult<Error> Validate(
        string city,
        string street,
        string house,
        string apartment)
    {
        var messages = new List<ErrorMessage>();

        ValidateField(city, nameof(City), "City", messages);
        ValidateField(street, nameof(Street), "Street", messages);
        ValidateField(house, nameof(House), "House", messages);
        ValidateField(apartment, nameof(Apartment), "Apartment", messages);

        return messages.Count > 0
            ? GeneralErrors.Validation(messages)
            : UnitResult.Success<Error>();
    }

    public override string ToString()
    {
        var parts = new List<string>();

        parts.Add(City);
        parts.Add(Street);
        parts.Add(House);
        parts.Add(Apartment);

        return string.Join(", ", parts);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return City;
        yield return Street;
        yield return House;
        yield return Apartment;
    }

    private static void ValidateField(
        string value,
        string propertyName,
        string displayName,
        List<ErrorMessage> messages)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            messages.Add(new("field.is.invalid", $"{displayName} is empty", propertyName));
            return;
        }

        var trimmed = value.Trim();

        if (trimmed.Length < MIN_PART_LENGTH || trimmed.Length > MAX_PART_LENGTH)
            messages.Add(new("field.is.invalid", $"{displayName} has invalid length", propertyName));
    }
}
