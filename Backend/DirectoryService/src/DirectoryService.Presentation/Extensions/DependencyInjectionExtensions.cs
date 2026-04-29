using DirectoryService.Application.Extensions;
using DirectoryService.Domain.Shared;
using DirectoryService.Infrastructure.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace DirectoryService.Presentation.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers();
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("ModelStateValidation");

                var errors = context.ModelState
                    .Where(pair => pair.Value is not null && pair.Value.Errors.Count > 0)
                    .SelectMany(pair => pair.Value!.Errors.Select(error => new ErrorMessage(
                        "field.is.invalid",
                        string.IsNullOrWhiteSpace(error.ErrorMessage)
                            ? "Request is invalid."
                            : error.ErrorMessage,
                        pair.Key)))
                    .ToArray();

                var domainError = Error.Validation(errors);

                logger.LogWarning(
                    "Model binding validation failed for {RequestMethod} {RequestPath}. Errors: {@ValidationErrors}",
                    context.HttpContext.Request.Method,
                    context.HttpContext.Request.Path.Value,
                    errors);

                return domainError.ToMvcResult();
            };
        });

        services.AddApplication();
        services.AddDatabase(configuration);

        return services;
    }
}
