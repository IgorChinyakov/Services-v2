using DirectoryService.Infrastructure.Database;
using DirectoryService.Presentation.Extensions;
using DirectoryService.Presentation.Middleware;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

var bootstrapEnvironmentName =
    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
    Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ??
    Environments.Production;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("EnvironmentName", bootstrapEnvironmentName)
    .Enrich.WithProperty("MachineName", Environment.MachineName)
    .WriteTo.Console(theme: AnsiConsoleTheme.Sixteen)
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting DirectoryService");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, loggerConfiguration) =>
    {
        loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty(
                "EnvironmentName",
                context.HostingEnvironment.EnvironmentName)
            .Enrich.WithProperty("MachineName", Environment.MachineName);

        var seqServerUrl = context.Configuration["Seq:ServerUrl"];

        if (!string.IsNullOrWhiteSpace(seqServerUrl))
        {
            loggerConfiguration.WriteTo.Seq(seqServerUrl);
        }
    });

    builder.Services.AddOpenApi();
    builder.Services.AddHealthChecks();

    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [];

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Frontend", policy =>
        {
            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });

    builder.Services.AddApplicationServices(builder.Configuration);

    var app = builder.Build();

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.GetLevel = (httpContext, _, exception) =>
        {
            if (exception is not null || httpContext.Response.StatusCode >= StatusCodes.Status500InternalServerError)
                return LogEventLevel.Error;

            if (httpContext.Response.StatusCode >= StatusCodes.Status400BadRequest)
                return LogEventLevel.Warning;

            return LogEventLevel.Information;
        };

        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
            diagnosticContext.Set(
                "RemoteIpAddress",
                httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
        };
    });

    app.UseMiddleware<ExceptionHandlingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();

        Log.Information("Applying database migrations");
        await dbContext.Database.MigrateAsync();
        Log.Information("Database migrations applied successfully");

        app.MapOpenApi();
        app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "DirectoryService"));
    }

    app.UseHttpsRedirection();

    app.UseCors("Frontend");

    app.MapControllers();
    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception exception) when (exception.GetType().Name != "HostAbortedException")
{
    Log.Fatal(exception, "DirectoryService terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

namespace DirectoryService.Api
{
    public partial class Program;
}
