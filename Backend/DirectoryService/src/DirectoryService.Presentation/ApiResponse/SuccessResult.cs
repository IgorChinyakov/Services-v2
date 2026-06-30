using System.Net;

namespace DirectoryService.Presentation.ApiResponse;

public class SuccessResult : IResult
{
    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var envelope = Envelope.Ok();

        httpContext.Response.StatusCode = (int)HttpStatusCode.OK;

        return httpContext.Response.WriteAsJsonAsync(envelope);
    }
}
