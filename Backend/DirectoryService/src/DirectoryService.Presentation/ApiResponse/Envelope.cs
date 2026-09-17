using System.Text.Json.Serialization;
using DirectoryService.Domain.Shared;

namespace DirectoryService.Presentation.ApiResponse;

public record Envelope
{
    public object? Result { get; }

    public IReadOnlyList<ErrorMessage> Errors { get; }

    public bool IsError => Errors.Count > 0;

    public DateTime TimeGenerated { get; }

    [JsonConstructor]
    private Envelope(object? result, IReadOnlyList<ErrorMessage> errors)
    {
        Result = result;
        Errors = errors;
        TimeGenerated = DateTime.UtcNow;
    }

    public static Envelope Ok(object? result = null) =>
        new(result, []);

    public static Envelope Fail(Error error) =>
        new(null, error.Messages);
}
