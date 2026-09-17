using System.Text.Json.Serialization;
using DirectoryService.Domain.Shared;

namespace DirectoryService.Presentation.ApiResponse;

#pragma warning disable SA1649
public record Envelope<T>
#pragma warning restore SA1649
{
    public T? Result { get; }

    public IReadOnlyList<ErrorMessage> Errors { get; }

    public bool IsError => Errors.Count > 0;

    public DateTime TimeGenerated { get; }

    [JsonConstructor]
    private Envelope(T? result, IReadOnlyList<ErrorMessage> errors)
    {
        Result = result;
        Errors = errors;
        TimeGenerated = DateTime.UtcNow;
    }

    public static Envelope<T> Ok(T? result = default) =>
        new(result, []);

    public static Envelope<T> Fail(Error error) =>
        new(default, error.Messages);
}
