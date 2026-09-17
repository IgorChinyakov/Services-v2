namespace DirectoryService.Presentation.ApiResponse;

#pragma warning disable SA1649
public class SuccessResult<TValue> : IResult
#pragma warning restore SA1649
{
    private readonly TValue _value;
    private readonly int _statusCode;

    public SuccessResult(TValue value, int statusCode = StatusCodes.Status200OK)
    {
        _value = value;
        _statusCode = statusCode;
    }

    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var envelope = Envelope.Ok(_value);

        httpContext.Response.StatusCode = _statusCode;

        return httpContext.Response.WriteAsJsonAsync(envelope);
    }
}
