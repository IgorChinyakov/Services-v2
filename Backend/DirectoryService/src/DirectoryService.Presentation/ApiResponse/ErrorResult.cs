using DirectoryService.Domain.Shared;

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

        int statusCode = GetStatusCodeFromErrorType(_error.Type);

        var envelope = Envelope.Fail(_error);
        httpContext.Response.StatusCode = statusCode;

        return httpContext.Response.WriteAsJsonAsync(envelope);
    }

    private static int GetStatusCodeFromErrorType(ErrorType errorType) =>
        errorType switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Failure => StatusCodes.Status500InternalServerError,
            ErrorType.Authentication => StatusCodes.Status401Unauthorized,
            ErrorType.Authorization => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError,
        };
}