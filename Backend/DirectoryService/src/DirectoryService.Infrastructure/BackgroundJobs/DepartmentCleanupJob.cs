using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Features.Departments.CleanupInactive;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace DirectoryService.Infrastructure.BackgroundJobs;

[DisallowConcurrentExecution]
public sealed class DepartmentCleanupJob : IJob
{
    public static readonly JobKey Key = new(nameof(DepartmentCleanupJob));

    private readonly ICommandHandler<CleanupInactiveDepartmentsCommand, int> _handler;
    private readonly DepartmentCleanupOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DepartmentCleanupJob> _logger;

    public DepartmentCleanupJob(
        ICommandHandler<CleanupInactiveDepartmentsCommand, int> handler,
        IOptions<DepartmentCleanupOptions> options,
        TimeProvider timeProvider,
        ILogger<DepartmentCleanupJob> logger)
    {
        _handler = handler;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var deletedBeforeUtc = _timeProvider
            .GetUtcNow()
            .UtcDateTime
            .AddDays(-_options.RetentionDays);

        var totalDeletedCount = 0;
        int deletedCount;

        do
        {
            var command = new CleanupInactiveDepartmentsCommand(
                deletedBeforeUtc,
                _options.BatchSize);

            var result = await _handler.Handle(command, context.CancellationToken);
            if (result.IsFailure)
            {
                _logger.LogError(
                    "Department cleanup job failed. ErrorType {ErrorType}. Errors {@Errors}",
                    result.Error.Type,
                    result.Error.Messages);

                throw new JobExecutionException("Department cleanup job failed.");
            }

            deletedCount = result.Value;
            totalDeletedCount += deletedCount;
        }
        while (deletedCount == _options.BatchSize);

        _logger.LogInformation(
            "Department cleanup job finished. DeletedCount {DeletedCount}. DeletedBeforeUtc {DeletedBeforeUtc}",
            totalDeletedCount,
            deletedBeforeUtc);
    }
}
