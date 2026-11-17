using FluentValidation;

namespace DirectoryService.Application.Features.Locations.Restore;

public sealed class RestoreLocationCommandValidator : AbstractValidator<RestoreLocationCommand>
{
    public RestoreLocationCommandValidator()
    {
        RuleFor(command => command.LocationId)
            .NotEmpty()
            .WithMessage("LocationId must not be empty");
    }
}
