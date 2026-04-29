using DirectoryService.Domain.Shared;
using DirectoryService.Presentation.ApiResponse;
using Microsoft.AspNetCore.Mvc;

namespace DirectoryService.Presentation.Extensions;

public static class ErrorMappingExtensions
{
    public static int ToStatusCode(this Error error)
    {
        return error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Authentication => StatusCodes.Status401Unauthorized,
            ErrorType.Authorization => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError,
        };
    }

    public static IActionResult ToMvcResult(this Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ObjectResult(Envelope.Fail(error))
        {
            StatusCode = error.ToStatusCode(),
        };
    }
}