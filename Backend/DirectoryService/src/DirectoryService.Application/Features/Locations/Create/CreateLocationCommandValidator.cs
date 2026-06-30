using DirectoryService.Application.Extensions.Validation;
using DirectoryService.Domain.Shared;
using DirectoryService.Domain.ValueObjects.Location;
using FluentValidation;

namespace DirectoryService.Application.Features.Locations.Create;

public sealed class CreateLocationCommandValidator : AbstractValidator<CreateLocationCommand>
{
    public CreateLocationCommandValidator()
    {
        RuleFor(x => x.Name)
            .MustBeValueObject(
                value => Name.Validate(value ?? string.Empty),
                (context, _) => context.PropertyPath);

        RuleFor(x => x.Timezone)
            .MustBeValueObject(
                value => LocationTimeZone.Validate(value ?? string.Empty),
                (context, _) => context.PropertyPath);

        RuleFor(x => x.Address)
            .MustBeValueObject(
                address => address is null
                    ? GeneralErrors.Validation("Address is required.", nameof(CreateLocationCommand.Address))
                    : Address.Validate(
                        address.Country,
                        address.City,
                        address.Street,
                        address.Building),
                (context, message) => ResolveAddressPropertyPath(context, message));
    }

    private static string ResolveAddressPropertyPath(
        ValidationContext<CreateLocationCommand> context,
        ErrorMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.InvalidField) ||
            string.Equals(message.InvalidField, nameof(CreateLocationCommand.Address), StringComparison.Ordinal))
        {
            return context.PropertyPath;
        }

        return $"{context.PropertyPath}.{message.InvalidField}";
    }
}
