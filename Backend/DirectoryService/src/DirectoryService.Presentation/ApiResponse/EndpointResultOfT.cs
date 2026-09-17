using CSharpFunctionalExtensions;
using DirectoryService.Domain.Shared;
using Microsoft.AspNetCore.Mvc;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace DirectoryService.Presentation.ApiResponse;

#pragma warning disable SA1649
public sealed class EndpointResult<TValue> : IResult, IActionResult
#pragma warning restore SA1649
{
    private readonly IResult _result;

    public EndpointResult(Result<TValue, Error> result)
        : this(result, StatusCodes.Status200OK)
    {
    }

    private EndpointResult(Result<TValue, Error> result, int successStatusCode)
    {
        _result = result.IsSuccess
            ? new SuccessResult<TValue>(result.Value, successStatusCode)
            : new ErrorResult(result.Error);
    }

    public Task ExecuteAsync(HttpContext httpContext) =>
        _result.ExecuteAsync(httpContext);

    public Task ExecuteResultAsync(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return ExecuteAsync(context.HttpContext);
    }

    public static implicit operator EndpointResult<TValue>(Result<TValue, Error> result) => new(result);

    public static EndpointResult<TValue> Created(Result<TValue, Error> result) =>
        new(result, StatusCodes.Status201Created);
}
