using DirectoryService.Application.Features.Departments.CleanupInactive;
using DirectoryService.Infrastructure.BackgroundJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Quartz;

namespace DirectoryService.Infrastructure.Extensions;

public static class DepartmentCleanupExtensions
{
    public static IServiceCollection AddDepartmentCleanup(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = ReadOptions(configuration);

        services.AddSingleton<IOptions<DepartmentCleanupOptions>>(Options.Create(options));

        services.TryAddSingleton(TimeProvider.System);

        services.AddQuartz(quartz =>
        {
            if (options.Enabled)
            {
                quartz.AddJob<DepartmentCleanupJob>(job => job.WithIdentity(DepartmentCleanupJob.Key));
                quartz.AddTrigger(trigger => trigger
                    .WithIdentity($"{nameof(DepartmentCleanupJob)}Trigger")
                    .ForJob(DepartmentCleanupJob.Key)
                    .WithCronSchedule(options.CronSchedule));
            }
        });

        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

        return services;
    }

    private static DepartmentCleanupOptions ReadOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection(DepartmentCleanupOptions.SectionName);
        var options = new DepartmentCleanupOptions();

        if (section[nameof(options.Enabled)] is { } enabledValue)
        {
            if (!bool.TryParse(enabledValue, out var enabled))
                throw new InvalidOperationException("DepartmentCleanup:Enabled must be true or false.");

            options.Enabled = enabled;
        }

        if (section[nameof(options.CronSchedule)] is { } cronSchedule)
            options.CronSchedule = cronSchedule;

        if (section[nameof(options.RetentionDays)] is { } retentionDaysValue)
        {
            if (!int.TryParse(retentionDaysValue, out var retentionDays))
                throw new InvalidOperationException("DepartmentCleanup:RetentionDays must be an integer.");

            options.RetentionDays = retentionDays;
        }

        if (section[nameof(options.BatchSize)] is { } batchSizeValue)
        {
            if (!int.TryParse(batchSizeValue, out var batchSize))
                throw new InvalidOperationException("DepartmentCleanup:BatchSize must be an integer.");

            options.BatchSize = batchSize;
        }

        if (options.Enabled && !CronExpression.IsValidExpression(options.CronSchedule))
            throw new InvalidOperationException("DepartmentCleanup:CronSchedule must be a valid Quartz cron expression.");

        if (options.RetentionDays <= 0)
            throw new InvalidOperationException("DepartmentCleanup:RetentionDays must be greater than zero.");

        if (options.BatchSize is < 1 or > CleanupInactiveDepartmentsCommandValidator.MaxBatchSize)
        {
            throw new InvalidOperationException(
                $"DepartmentCleanup:BatchSize must be between 1 and {CleanupInactiveDepartmentsCommandValidator.MaxBatchSize}.");
        }

        return options;
    }
}
