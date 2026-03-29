using System.Text.Json.Serialization;

namespace DirectoryService.Domain.Shared;

public record Error
{
    public IReadOnlyList<ErrorMessage> Messages { get; } = [];

    public ErrorType Type { get; }

    [JsonConstructor]
    private Error(IReadOnlyList<ErrorMessage> messages, ErrorType type)
    {
        Messages = messages.ToArray();
        Type = type;
    }

    private Error(IEnumerable<ErrorMessage> messages, ErrorType type)
    {
        Messages = messages.ToArray();
        Type = type;
    }

    public string GetMessage() => string.Join(";", Messages.Select(m => m.ToString()));

    public static Error Validation(string message, string? invalidField = null) =>
        new([new ErrorMessage("field.is.invalid", message, invalidField)], ErrorType.Validation);

    public static Error NotFound(string message, string? invalidField = null) =>
        new([new ErrorMessage("not.found", message, invalidField)], ErrorType.NotFound);

    public static Error Failure(string message, string? invalidField = null) =>
        new([new ErrorMessage("failure", message, invalidField)], ErrorType.Failure);

    public static Error Conflict(string message, string? invalidField = null) =>
        new([new ErrorMessage("conflict", message, invalidField)], ErrorType.Conflict);

    public static Error Authentication(string message, string? invalidField = null) =>
        new([new ErrorMessage("authentication", message, invalidField)], ErrorType.Authentication);

    public static Error Authorization(string message, string? invalidField = null) =>
        new([new ErrorMessage("authorization", message, invalidField)], ErrorType.Authorization);

    public static Error Validation(params IEnumerable<ErrorMessage> messages) =>
        new(messages, ErrorType.Validation);

    public static Error NotFound(params IEnumerable<ErrorMessage> messages) =>
        new(messages, ErrorType.NotFound);

    public static Error Failure(params IEnumerable<ErrorMessage> messages) =>
        new(messages, ErrorType.Failure);

    public static Error Conflict(params IEnumerable<ErrorMessage> messages) =>
        new(messages, ErrorType.Conflict);

    public static Error Authentication(params IEnumerable<ErrorMessage> messages) =>
        new(messages, ErrorType.Authentication);

    public static Error Authorization(params IEnumerable<ErrorMessage> messages) =>
        new(messages, ErrorType.Authorization);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ErrorType
{
    Validation,
    NotFound,
    Failure,
    Conflict,
    Authentication,
    Authorization,
}