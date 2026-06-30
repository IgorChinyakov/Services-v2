namespace DirectoryService.Presentation.ApiResponse;

#pragma warning disable SA1649
public class SuccessResult<TValue> : IResult
#pragma warning restore SA1649
{
    private readonly TValue _value;

    public SuccessResult(TValue value)
    {
        _value = value;
    }

    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var envelope = Envelope.Ok(_value);

        httpContext.Response.StatusCode = StatusCodes.Status200OK;

        return httpContext.Response.WriteAsJsonAsync(envelope);
    }
}
