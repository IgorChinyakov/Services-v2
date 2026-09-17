using DirectoryService.Domain.Shared;
using DirectoryService.Presentation.Extensions;

namespace DirectoryService.Presentation.ApiResponse;

public class ErrorResult : IResult
{
    private readonly Error _error;

    public ErrorResult(Error error)
    {
        _error = error;
    }

    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var envelope = Envelope.Fail(_error);
        httpContext.Response.StatusCode = _error.ToStatusCode();

        return httpContext.Response.WriteAsJsonAsync(envelope);
    }
}
