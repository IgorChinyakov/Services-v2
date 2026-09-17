using FluentValidation;

namespace DirectoryService.Application.Features.Positions.CleanupInactive;

public sealed class CleanupInactivePositionsCommandValidator
    : AbstractValidator<CleanupInactivePositionsCommand>
{
    public const int MaxBatchSize = 1_000;

    public CleanupInactivePositionsCommandValidator()
    {
        RuleFor(command => command.DeletedBeforeUtc)
            .NotEmpty()
            .Must(value => value.Kind == DateTimeKind.Utc)
            .WithMessage("DeletedBeforeUtc must be in UTC.");

        RuleFor(command => command.BatchSize)
            .InclusiveBetween(1, MaxBatchSize);
    }
}
