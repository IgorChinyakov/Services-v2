namespace DirectoryService.Infrastructure.BackgroundJobs;

public sealed class PositionCleanupOptions
{
    public const string SectionName = "PositionCleanup";

    public bool Enabled { get; set; } = true;

    public string CronSchedule { get; set; } = "0 0 2 * * ?";

    public int RetentionDays { get; set; } = 30;

    public int BatchSize { get; set; } = 100;
}
