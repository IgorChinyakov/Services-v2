using DirectoryService.Application.Extensions.Validation;
using DirectoryService.Domain.ValueObjects.Position;
using FluentValidation;

namespace DirectoryService.Application.Features.Positions.Update;

public sealed class UpdatePositionCommandValidator : AbstractValidator<UpdatePositionCommand>
{
    public UpdatePositionCommandValidator()
    {
        RuleFor(command => command.PositionId)
            .NotEmpty()
            .WithMessage("PositionId must not be empty");

        RuleFor(command => command.Name)
            .MustBeValueObject(
                value => Name.Validate(value ?? string.Empty),
                (context, _) => context.PropertyPath);
    }
}
