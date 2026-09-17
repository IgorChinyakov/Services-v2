using DirectoryService.Application.Abstractions.Handlers;
using DirectoryService.Application.Features.Locations.CleanupInactive;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace DirectoryService.Infrastructure.BackgroundJobs;

[DisallowConcurrentExecution]
public sealed class LocationCleanupJob : IJob
{
    public static readonly JobKey Key = new(nameof(LocationCleanupJob));

    private readonly ICommandHandler<CleanupInactiveLocationsCommand, int> _handler;
    private readonly LocationCleanupOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<LocationCleanupJob> _logger;

    public LocationCleanupJob(
        ICommandHandler<CleanupInactiveLocationsCommand, int> handler,
        IOptions<LocationCleanupOptions> options,
        TimeProvider timeProvider,
        ILogger<LocationCleanupJob> logger)
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
            var command = new CleanupInactiveLocationsCommand(
                deletedBeforeUtc,
                _options.BatchSize);

            var result = await _handler.Handle(command, context.CancellationToken);
            if (result.IsFailure)
            {
                _logger.LogError(
                    "Location cleanup job failed. ErrorType {ErrorType}. Errors {@Errors}",
                    result.Error.Type,
                    result.Error.Messages);

                throw new JobExecutionException("Location cleanup job failed.");
            }

            deletedCount = result.Value;
            totalDeletedCount += deletedCount;
        }
        while (deletedCount == _options.BatchSize);

        _logger.LogInformation(
            "Location cleanup job finished. DeletedCount {DeletedCount}. DeletedBeforeUtc {DeletedBeforeUtc}",
            totalDeletedCount,
            deletedBeforeUtc);
    }
}
