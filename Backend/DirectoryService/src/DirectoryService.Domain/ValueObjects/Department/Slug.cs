using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;
using DirectoryService.Domain.Shared;

namespace DirectoryService.Domain.ValueObjects.Department;

public partial class Slug : ValueObject
{
    public const int MIN_LENGTH = 3;
    public const int MAX_LENGTH = 150;

    private static readonly Regex _slugRegex = SlugRegex();

    private Slug(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<Slug, Error> Create(string value)
    {
        var validationResult = Validate(value);
        if (validationResult.IsFailure)
            return validationResult.Error;

        return new Slug(value.Trim());
    }

    public static UnitResult<Error> Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return GeneralErrors.Validation("Department slug is empty", nameof(Slug));

        var trimmed = value.Trim();
        var messages = new List<ErrorMessage>();

        if (trimmed.Length < MIN_LENGTH || trimmed.Length > MAX_LENGTH)
        {
            messages.Add(new ErrorMessage(
                "field.is.invalid",
                $"Department slug must be between {MIN_LENGTH} and {MAX_LENGTH} characters",
                nameof(Slug)));
        }

        if (!_slugRegex.IsMatch(trimmed))
        {
            messages.Add(new ErrorMessage(
                "field.is.invalid",
                "Department slug must contain lowercase Latin letters, digits, and single hyphens only",
                nameof(Slug)));
        }

        return messages.Count > 0
            ? GeneralErrors.Validation(messages)
            : UnitResult.Success<Error>();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled)]
    private static partial Regex SlugRegex();
}
