using DirectoryService.Domain.Shared;

namespace DirectoryService.Domain.Errors;

public static class PositionErrors
{
    public static Error NotFound(Guid? positionId = null, string? invalidField = null)
    {
        var message = positionId is null
            ? "Position was not found or is inactive."
            : $"Position with id '{positionId}' was not found or is inactive.";

        return Error.NotFound("position.not_found", message, invalidField);
    }

    public static Error NameDuplicate(string? invalidField = null) =>
        Error.Conflict(
            "position.name.duplicate",
            "Active position with the same name already exists.",
            invalidField);
}
