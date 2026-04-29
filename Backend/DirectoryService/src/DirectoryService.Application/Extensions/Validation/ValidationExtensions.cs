using CSharpFunctionalExtensions;
using DirectoryService.Domain.Shared;
using FluentValidation;
using FluentValidation.Results;

namespace DirectoryService.Application.Extensions.Validation;

public static class ValidationExtensions
{
    public static Error ToError(this ValidationResult validationResult)
    {
        return Error.Validation(validationResult.Errors.Select(failure =>
            new ErrorMessage(
                "field.is.invalid",
                failure.ErrorMessage,
                failure.PropertyName)));
    }

    public static IRuleBuilderOptionsConditions<T, TElement?> MustBeValueObject<T, TElement>(
        this IRuleBuilder<T, TElement?> ruleBuilder,
        Func<TElement?, UnitResult<Error>> factoryMethod,
        Func<ValidationContext<T>, ErrorMessage, string?>? propertyNameFactory = null)
    {
        return ruleBuilder.Custom((value, context) =>
        {
            var result = factoryMethod(value);

            if (result.IsSuccess)
                return;

            foreach (var message in result.Error.Messages)
            {
                var propertyName = propertyNameFactory?.Invoke(context, message) ?? context.PropertyPath;

                if (string.IsNullOrWhiteSpace(propertyName))
                {
                    context.AddFailure(message.Message);
                    continue;
                }

                context.AddFailure(propertyName, message.Message);
            }
        });
    }
}