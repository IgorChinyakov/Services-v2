namespace DirectoryService.Domain.Shared;

public static class GeneralErrors
{
    public static Error Validation(string message, string? invalidField = null) =>
        Error.Validation("field.is.invalid", message, invalidField);

    public static Error NotFound(string message, string? invalidField = null) =>
        Error.NotFound("not.found", message, invalidField);

    public static Error Failure(string message, string? invalidField = null) =>
        Error.Failure("failure", message, invalidField);

    public static Error Conflict(string message, string? invalidField = null) =>
        Error.Conflict("conflict", message, invalidField);

    public static Error Authentication(string message, string? invalidField = null) =>
        Error.Authentication("authentication", message, invalidField);

    public static Error Authorization(string message, string? invalidField = null) =>
        Error.Authorization("authorization", message, invalidField);

    public static Error Validation(IEnumerable<ErrorMessage> messages) =>
        Error.Validation(messages);

    public static Error NotFound(IEnumerable<ErrorMessage> messages) =>
        Error.Create(messages, ErrorType.NotFound);

    public static Error Failure(IEnumerable<ErrorMessage> messages) =>
        Error.Create(messages, ErrorType.Failure);

    public static Error Conflict(IEnumerable<ErrorMessage> messages) =>
        Error.Create(messages, ErrorType.Conflict);

    public static Error Authentication(IEnumerable<ErrorMessage> messages) =>
        Error.Create(messages, ErrorType.Authentication);

    public static Error Authorization(IEnumerable<ErrorMessage> messages) =>
        Error.Create(messages, ErrorType.Authorization);
}
