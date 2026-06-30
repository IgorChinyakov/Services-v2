namespace DirectoryService.Domain.Shared;

public static class GeneralErrors
{
    public static Error Validation(string message, string? invalidField = null) =>
        Error.Create([new ErrorMessage("field.is.invalid", message, invalidField)], ErrorType.Validation);

    public static Error NotFound(string message, string? invalidField = null) =>
        Error.Create([new ErrorMessage("not.found", message, invalidField)], ErrorType.NotFound);

    public static Error Failure(string message, string? invalidField = null) =>
        Error.Create([new ErrorMessage("failure", message, invalidField)], ErrorType.Failure);

    public static Error Conflict(string message, string? invalidField = null) =>
        Error.Create([new ErrorMessage("conflict", message, invalidField)], ErrorType.Conflict);

    public static Error Authentication(string message, string? invalidField = null) =>
        Error.Create([new ErrorMessage("authentication", message, invalidField)], ErrorType.Authentication);

    public static Error Authorization(string message, string? invalidField = null) =>
        Error.Create([new ErrorMessage("authorization", message, invalidField)], ErrorType.Authorization);

    public static Error Validation(IEnumerable<ErrorMessage> messages) =>
        Error.Create(messages, ErrorType.Validation);

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
