using CSharpFunctionalExtensions;
using DirectoryService.Application.Extensions;
using DirectoryService.Domain.Shared;
using DirectoryService.Infrastructure.Extensions;
using DirectoryService.Presentation.ApiResponse;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Hybrid;
using StackExchange.Redis;

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

                var domainError = GeneralErrors.Validation(errors);

                logger.LogWarning(
                    "Model binding validation failed for {RequestMethod} {RequestPath}. Errors: {@ValidationErrors}",
                    context.HttpContext.Request.Method,
                    context.HttpContext.Request.Path.Value,
                    errors);

                return EndpointResult.ToEndpointResult(UnitResult.Failure(domainError));
            };
        });

        services.AddApplication();
        services.AddDatabase(configuration);
        services.AddDepartmentCleanup(configuration);
        services.AddLocationCleanup(configuration);
        services.AddPositionCleanup(configuration);

        var redisEnabled = configuration.GetValue("Redis:Enabled", true);
        if (redisEnabled)
        {
            string redisConnectionString = configuration["Redis:ConnectionString"] ??
                                           throw new InvalidOperationException(
                                               "Redis connection string is not configured.");

            var redisOptions = ConfigurationOptions.Parse(redisConnectionString);
            redisOptions.AbortOnConnectFail = false;
            redisOptions.ConnectRetry = 1;
            redisOptions.ConnectTimeout = configuration.GetValue(
                "Redis:ConnectTimeoutMilliseconds",
                1000);
            redisOptions.AsyncTimeout = configuration.GetValue(
                "Redis:OperationTimeoutMilliseconds",
                1000);
            redisOptions.SyncTimeout = redisOptions.AsyncTimeout;

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisOptions.ToString(includePassword: true);
            });
        }

        services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new HybridCacheEntryOptions()
            {
                LocalCacheExpiration = TimeSpan.FromMinutes(3), Expiration = TimeSpan.FromMinutes(5),
            };
        });

        return services;
    }
}
