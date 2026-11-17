using FluentValidation;

namespace DirectoryService.Application.Features.Departments.Bulk;

public sealed class BulkDepartmentCommandValidator : AbstractValidator<BulkDepartmentCommand>
{
    public const int MaxCount = 100;

    public BulkDepartmentCommandValidator()
    {
        RuleFor(command => command.Ids)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(ids => ids!.Length <= MaxCount)
            .WithMessage($"Select no more than {MaxCount} departments.")
            .Must(ids => ids!.Distinct().Count() == ids!.Length)
            .WithMessage("Department IDs must be unique.");

        When(command => command.Ids is not null, () =>
        {
            RuleForEach(command => command.Ids!).NotEmpty();
        });

        RuleFor(command => command.Action).IsInEnum();
        RuleFor(command => command.IsActive)
            .NotNull()
            .When(command => command.Action == DepartmentBulkAction.Activity);
    }
}
