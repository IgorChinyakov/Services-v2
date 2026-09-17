using CSharpFunctionalExtensions;
using DirectoryService.Domain.Shared;
using Microsoft.AspNetCore.Mvc;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace DirectoryService.Presentation.ApiResponse;

public sealed class EndpointResult : IResult, IActionResult
{
    private readonly IResult _result;

    public EndpointResult(UnitResult<Error> result)
    {
        _result = result.IsSuccess
            ? new SuccessResult()
            : new ErrorResult(result.Error);
    }

    public Task ExecuteAsync(HttpContext httpContext) =>
        _result.ExecuteAsync(httpContext);

    public Task ExecuteResultAsync(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return ExecuteAsync(context.HttpContext);
    }

    public static implicit operator EndpointResult(UnitResult<Error> result) => new(result);

    public static EndpointResult ToEndpointResult(UnitResult<Error> result) => new(result);
}
