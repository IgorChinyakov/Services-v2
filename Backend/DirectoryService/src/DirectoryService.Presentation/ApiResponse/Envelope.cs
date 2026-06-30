using System.Text.Json.Serialization;
using DirectoryService.Domain.Shared;

namespace DirectoryService.Presentation.ApiResponse;

public record Envelope
{
    public object? Result { get; }

    public Error? Error { get; }

    public bool IsError => Error != null;

    public DateTime TimeGenerated { get; }

    [JsonConstructor]
    private Envelope(object? result, Error? error)
    {
        Result = result;
        Error = error;
        TimeGenerated = DateTime.UtcNow;
    }

    public static Envelope Ok(object? result = null) =>
        new(result, null);

    public static Envelope Fail(Error error) =>
        new(null, error);
}
