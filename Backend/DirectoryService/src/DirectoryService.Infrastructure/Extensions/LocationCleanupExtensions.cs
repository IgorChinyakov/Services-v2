using DirectoryService.Application.Features.Locations.CleanupInactive;
using DirectoryService.Infrastructure.BackgroundJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Quartz;

namespace DirectoryService.Infrastructure.Extensions;

public static class LocationCleanupExtensions
{
    public static IServiceCollection AddLocationCleanup(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = ReadOptions(configuration);

        services.AddSingleton<IOptions<LocationCleanupOptions>>(Options.Create(options));

        services.AddQuartz(quartz =>
        {
            if (!options.Enabled)
                return;

            quartz.AddJob<LocationCleanupJob>(job => job.WithIdentity(LocationCleanupJob.Key));
            quartz.AddTrigger(trigger => trigger
                .WithIdentity($"{nameof(LocationCleanupJob)}Trigger")
                .ForJob(LocationCleanupJob.Key)
                .WithCronSchedule(options.CronSchedule));
        });

        return services;
    }

    private static LocationCleanupOptions ReadOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection(LocationCleanupOptions.SectionName);
        var options = new LocationCleanupOptions();

        if (section[nameof(options.Enabled)] is { } enabledValue)
        {
            if (!bool.TryParse(enabledValue, out var enabled))
                throw new InvalidOperationException("LocationCleanup:Enabled must be true or false.");

            options.Enabled = enabled;
        }

        if (section[nameof(options.CronSchedule)] is { } cronSchedule)
            options.CronSchedule = cronSchedule;

        if (section[nameof(options.RetentionDays)] is { } retentionDaysValue)
        {
            if (!int.TryParse(retentionDaysValue, out var retentionDays))
                throw new InvalidOperationException("LocationCleanup:RetentionDays must be an integer.");

            options.RetentionDays = retentionDays;
        }

        if (section[nameof(options.BatchSize)] is { } batchSizeValue)
        {
            if (!int.TryParse(batchSizeValue, out var batchSize))
                throw new InvalidOperationException("LocationCleanup:BatchSize must be an integer.");

            options.BatchSize = batchSize;
        }

        if (options.Enabled && !CronExpression.IsValidExpression(options.CronSchedule))
            throw new InvalidOperationException("LocationCleanup:CronSchedule must be a valid Quartz cron expression.");

        if (options.RetentionDays <= 0)
            throw new InvalidOperationException("LocationCleanup:RetentionDays must be greater than zero.");

        if (options.BatchSize is < 1 or > CleanupInactiveLocationsCommandValidator.MaxBatchSize)
        {
            throw new InvalidOperationException(
                $"LocationCleanup:BatchSize must be between 1 and {CleanupInactiveLocationsCommandValidator.MaxBatchSize}.");
        }

        return options;
    }
}
