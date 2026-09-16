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

    public static Error Validation(string code, string message, string? invalidField = null) =>
        Create([new ErrorMessage(code, message, invalidField)], ErrorType.Validation);

    public static Error Validation(IEnumerable<ErrorMessage> messages) =>
        Create(messages, ErrorType.Validation);

    public static Error NotFound(string code, string message, string? invalidField = null) =>
        Create([new ErrorMessage(code, message, invalidField)], ErrorType.NotFound);

    public static Error Conflict(string code, string message, string? invalidField = null) =>
        Create([new ErrorMessage(code, message, invalidField)], ErrorType.Conflict);

    public static Error Failure(string code, string message, string? invalidField = null) =>
        Create([new ErrorMessage(code, message, invalidField)], ErrorType.Failure);

    public static Error Authentication(string code, string message, string? invalidField = null) =>
        Create([new ErrorMessage(code, message, invalidField)], ErrorType.Authentication);

    public static Error Authorization(string code, string message, string? invalidField = null) =>
        Create([new ErrorMessage(code, message, invalidField)], ErrorType.Authorization);

    internal static Error Create(IEnumerable<ErrorMessage> messages, ErrorType type) =>
        new(messages, type);
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
