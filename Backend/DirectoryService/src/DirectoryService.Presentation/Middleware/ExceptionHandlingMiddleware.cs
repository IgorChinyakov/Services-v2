using CSharpFunctionalExtensions;
using DirectoryService.Domain.Shared;
using DirectoryService.Presentation.ApiResponse;
using DirectoryService.Presentation.Extensions;
using FluentValidation;

namespace DirectoryService.Presentation.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException exception) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogWarning(
                exception,
                "HTTP request {RequestMethod} {RequestPath} was cancelled by the client. TraceId {TraceId}",
                context.Request.Method,
                context.Request.Path.Value,
                context.TraceIdentifier);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogWarning(
                    exception,
                    "The response has already started, exception middleware cannot handle the error. TraceId {TraceId}",
                    context.TraceIdentifier);

                throw;
            }

            var error = MapException(exception);
            var statusCode = error.ToStatusCode();
            var endpointResult = EndpointResult.ToEndpointResult(UnitResult.Failure(error));

            if (statusCode >= StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(
                    exception,
                    "Unhandled exception for {RequestMethod} {RequestPath}. StatusCode {StatusCode}. TraceId {TraceId}",
                    context.Request.Method,
                    context.Request.Path.Value,
                    statusCode,
                    context.TraceIdentifier);
            }
            else
            {
                _logger.LogWarning(
                    exception,
                    "Handled exception for {RequestMethod} {RequestPath}. StatusCode {StatusCode}. TraceId {TraceId}",
                    context.Request.Method,
                    context.Request.Path.Value,
                    statusCode,
                    context.TraceIdentifier);
            }

            context.Response.Clear();
            await endpointResult.ExecuteAsync(context);
        }
    }

    private static Error MapException(Exception exception)
    {
        return exception switch
        {
            ValidationException validationException =>
                Error.Validation(validationException.Errors.Select(error =>
                    new ErrorMessage(
                        "field.is.invalid",
                        error.ErrorMessage,
                        error.PropertyName))),

            BadHttpRequestException =>
                Error.Validation("The HTTP request is invalid."),

            ArgumentException argumentException =>
                Error.Validation(argumentException.Message),

            UnauthorizedAccessException =>
                Error.Authorization("Access denied."),

            KeyNotFoundException keyNotFoundException =>
                Error.NotFound(keyNotFoundException.Message),

            _ => Error.Failure("An unexpected error occurred."),
        };
    }
}
