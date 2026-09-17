using FluentValidation;

namespace DirectoryService.Application.Features.Positions.Delete;

public sealed class DeletePositionCommandValidator : AbstractValidator<DeletePositionCommand>
{
    public DeletePositionCommandValidator()
    {
        RuleFor(command => command.PositionId)
            .NotEmpty()
            .WithMessage("PositionId must not be empty");
    }
}
