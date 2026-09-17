using FluentValidation;

namespace DirectoryService.Application.Features.Locations.CleanupInactive;

public sealed class CleanupInactiveLocationsCommandValidator
    : AbstractValidator<CleanupInactiveLocationsCommand>
{
    public const int MaxBatchSize = 1_000;

    public CleanupInactiveLocationsCommandValidator()
    {
        RuleFor(command => command.DeletedBeforeUtc)
            .NotEmpty()
            .Must(value => value.Kind == DateTimeKind.Utc)
            .WithMessage("DeletedBeforeUtc must be in UTC.");

        RuleFor(command => command.BatchSize)
            .InclusiveBetween(1, MaxBatchSize);
    }
}
