using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;
using DirectoryService.Domain.Shared;

namespace DirectoryService.Domain.ValueObjects.Department;

public partial class Identifier : ValueObject
{
    public string Value { get; }

    public const int MIN_LENGTH = 3;
    public const int MAX_LENGTH = 150;

    private static readonly Regex _identifierRegex = IdentifierRegex();

    private Identifier(string value)
    {
        Value = value;
    }

    public static Result<Identifier, Error> Create(string value)
    {
        var validationResult = Validate(value);
        if (validationResult.IsFailure)
            return validationResult.Error;

        return new Identifier(value.Trim());
    }

    public static UnitResult<Error> Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Error.Validation(
                "Department identifier is empty",
                "Department identifier");
        }

        var trimmed = value.Trim();
        var messages = new List<ErrorMessage>();
        if (trimmed.Length < MIN_LENGTH || trimmed.Length > MAX_LENGTH)
            messages.Add(new("DepartmentIdentifier has invalid length", "Department identifier"));

        if (!_identifierRegex.IsMatch(trimmed))
            messages.Add(new("DepartmentIdentifier must contain latin letters only", "Department identifier"));

        return messages.Count > 0
            ? Error.Validation(messages)
            : UnitResult.Success<Error>();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    [GeneratedRegex("^[A-Za-z]+$", RegexOptions.Compiled)]
    private static partial Regex IdentifierRegex();
}