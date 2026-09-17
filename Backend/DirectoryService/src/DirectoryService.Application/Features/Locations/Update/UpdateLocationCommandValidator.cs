using DirectoryService.Application.Extensions.Validation;
using DirectoryService.Domain.Shared;
using DirectoryService.Domain.ValueObjects.Location;
using FluentValidation;

namespace DirectoryService.Application.Features.Locations.Update;

public sealed class UpdateLocationCommandValidator : AbstractValidator<UpdateLocationCommand>
{
    public UpdateLocationCommandValidator()
    {
        RuleFor(command => command.LocationId)
            .NotEmpty()
            .WithMessage("LocationId must not be empty");

        RuleFor(command => command.Name)
            .MustBeValueObject(
                value => Name.Validate(value ?? string.Empty),
                (context, _) => context.PropertyPath);

        RuleFor(command => command.Address)
            .MustBeValueObject(
                address => address is null
                    ? GeneralErrors.Validation("Address is required.", nameof(UpdateLocationCommand.Address))
                    : Address.Validate(
                        address.City,
                        address.Street,
                        address.House,
                        address.Apartment),
                (context, message) => ResolveAddressPropertyPath(context, message));
    }

    private static string ResolveAddressPropertyPath(
        ValidationContext<UpdateLocationCommand> context,
        ErrorMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.InvalidField) ||
            string.Equals(message.InvalidField, nameof(UpdateLocationCommand.Address), StringComparison.Ordinal))
        {
            return context.PropertyPath;
        }

        return $"{context.PropertyPath}.{message.InvalidField}";
    }
}
