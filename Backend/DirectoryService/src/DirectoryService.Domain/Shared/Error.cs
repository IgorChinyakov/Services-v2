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
