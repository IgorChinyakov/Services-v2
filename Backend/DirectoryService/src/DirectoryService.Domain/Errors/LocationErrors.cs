using DirectoryService.Domain.Shared;

namespace DirectoryService.Domain.Errors;

public static class LocationErrors
{
    public static Error NotFound(Guid? locationId = null, string? invalidField = null)
    {
        var message = locationId is null
            ? "Location was not found or is inactive."
            : $"Location with id '{locationId}' was not found or is inactive.";

        return Error.NotFound("location.not_found", message, invalidField);
    }

    public static Error NotFound(IEnumerable<Guid> locationIds, string? invalidField = null)
    {
        var ids = string.Join(", ", locationIds);

        return Error.NotFound(
            "location.not_found",
            $"Locations were not found or are inactive: {ids}.",
            invalidField);
    }

    public static Error NameDuplicate(string? invalidField = null) =>
        Error.Conflict(
            "location.name.duplicate",
            "Location with the same name already exists.",
            invalidField);

    public static Error AddressDuplicate(string? invalidField = null) =>
        Error.Conflict(
            "location.address.duplicate",
            "Location with the same address already exists.",
            invalidField);
}
