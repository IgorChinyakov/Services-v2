using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Features.Positions.CleanupInactive;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace DirectoryService.Infrastructure.BackgroundJobs;

[DisallowConcurrentExecution]
public sealed class PositionCleanupJob : IJob
{
    public static readonly JobKey Key = new(nameof(PositionCleanupJob));

    private readonly ICommandHandler<CleanupInactivePositionsCommand, int> _handler;
    private readonly PositionCleanupOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<PositionCleanupJob> _logger;

    public PositionCleanupJob(
        ICommandHandler<CleanupInactivePositionsCommand, int> handler,
        IOptions<PositionCleanupOptions> options,
        TimeProvider timeProvider,
        ILogger<PositionCleanupJob> logger)
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
            var command = new CleanupInactivePositionsCommand(
                deletedBeforeUtc,
                _options.BatchSize);

            var result = await _handler.Handle(command, context.CancellationToken);
            if (result.IsFailure)
            {
                _logger.LogError(
                    "Position cleanup job failed. ErrorType {ErrorType}. Errors {@Errors}",
                    result.Error.Type,
                    result.Error.Messages);

                throw new JobExecutionException("Position cleanup job failed.");
            }

            deletedCount = result.Value;
            totalDeletedCount += deletedCount;
        }
        while (deletedCount == _options.BatchSize);

        _logger.LogInformation(
            "Position cleanup job finished. DeletedCount {DeletedCount}. DeletedBeforeUtc {DeletedBeforeUtc}",
            totalDeletedCount,
            deletedBeforeUtc);
    }
}
