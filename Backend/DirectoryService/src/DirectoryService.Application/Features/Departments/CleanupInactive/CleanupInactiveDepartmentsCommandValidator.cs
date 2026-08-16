using FluentValidation;

namespace DirectoryService.Application.Features.Departments.CleanupInactive;

public sealed class CleanupInactiveDepartmentsCommandValidator
    : AbstractValidator<CleanupInactiveDepartmentsCommand>
{
    public const int MaxBatchSize = 1_000;

    public CleanupInactiveDepartmentsCommandValidator()
    {
        RuleFor(command => command.DeletedBeforeUtc)
            .NotEmpty()
            .Must(value => value.Kind == DateTimeKind.Utc)
            .WithMessage("DeletedBeforeUtc must be in UTC.");

        RuleFor(command => command.BatchSize)
            .InclusiveBetween(1, MaxBatchSize);
    }
}
