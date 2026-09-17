using DirectoryService.Domain.Shared;

namespace DirectoryService.Domain.Errors;

public static class DepartmentErrors
{
    public static Error NotFound(Guid? departmentId = null, string? invalidField = null)
    {
        var message = departmentId is null
            ? "Department was not found or is inactive."
            : $"Department with id '{departmentId}' was not found or is inactive.";

        return Error.NotFound("department.not_found", message, invalidField);
    }

    public static Error NotFound(IEnumerable<Guid> departmentIds, string? invalidField = null)
    {
        var ids = string.Join(", ", departmentIds);

        return Error.NotFound(
            "department.not_found",
            $"Departments were not found or are inactive: {ids}.",
            invalidField);
    }

    public static Error ParentNotFound(Guid? parentId = null, string? invalidField = null)
    {
        var message = parentId is null
            ? "Parent department was not found or is inactive."
            : $"Parent department with id '{parentId}' was not found or is inactive.";

        return Error.NotFound("department.parent.not_found", message, invalidField);
    }

    public static Error SlugDuplicate(string? invalidField = null) =>
        Error.Conflict(
            "department.slug.duplicate",
            "Department with the same slug already exists.",
            invalidField);

    public static Error PathDuplicate() =>
        Error.Conflict(
            "department.path.duplicate",
            "Department with the same path already exists.");

    public static Error ParentIsSelf(string? invalidField = null) =>
        Error.Conflict(
            "department.parent.self",
            "Department cannot be its own parent.",
            invalidField);

    public static Error ParentIsDescendant(string? invalidField = null) =>
        Error.Conflict(
            "department.parent.descendant",
            "Department cannot be moved under its own descendant.",
            invalidField);

    public static Error LocationAlreadyLinked() =>
        Error.Conflict(
            "department.location.duplicate",
            "Location is already linked to the department.");

    public static Error LocationNotLinked() =>
        Error.NotFound(
            "department.location.not_found",
            "Location is not linked to the department.");

    public static Error PositionAlreadyLinked() =>
        Error.Conflict(
            "department.position.duplicate",
            "Position is already linked to the department.");

    public static Error PositionNotLinked() =>
        Error.NotFound(
            "department.position.not_found",
            "Position is not linked to the department.");
}
