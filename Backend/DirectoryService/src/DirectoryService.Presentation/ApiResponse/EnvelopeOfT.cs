using System.Text.Json.Serialization;
using DirectoryService.Domain.Shared;

namespace DirectoryService.Presentation.ApiResponse;

#pragma warning disable SA1649
public record Envelope<T>
#pragma warning restore SA1649
{
    public T? Result { get; }

    public Error? Error { get; }

    public bool IsError => Error != null;

    public DateTime TimeGenerated { get; }

    [JsonConstructor]
    private Envelope(T? result, Error? error)
    {
        Result = result;
        Error = error;
        TimeGenerated = DateTime.Now;
    }

    public static Envelope<T> Ok(T? result = default) =>
        new(result, null);

    public static Envelope<T> Fail(Error error) =>
        new(default, error);
}
