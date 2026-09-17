using FluentValidation;

namespace DirectoryService.Application.Features.Locations.Delete;

public sealed class DeleteLocationCommandValidator : AbstractValidator<DeleteLocationCommand>
{
    public DeleteLocationCommandValidator()
    {
        RuleFor(command => command.LocationId)
            .NotEmpty()
            .WithMessage("LocationId must not be empty");
    }
}
