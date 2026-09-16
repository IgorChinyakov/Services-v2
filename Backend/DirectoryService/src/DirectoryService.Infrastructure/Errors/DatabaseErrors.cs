using DirectoryService.Domain.Errors;
using DirectoryService.Domain.Shared;
using Npgsql;

namespace DirectoryService.Infrastructure.Errors;

public static class DatabaseErrors
{
    public static Error OperationFailed(string operation) =>
        Error.Failure("database.operation_failed", $"Failed to {operation}.");

    public static Error Timeout(string operation) =>
        Error.Failure("database.timeout", $"The database timed out while {operation}.");

    public static Error Unavailable(string operation) =>
        Error.Failure("database.unavailable", $"The database is unavailable. Failed to {operation}.");

    public static Error SaveFailed(string entityName) =>
        Error.Failure("database.save_failed", $"Failed to save the {entityName} to the database.");

    public static Error FromPostgresException(
        PostgresException exception,
        string entityName)
    {
        return exception.SqlState switch
        {
            PostgresErrorCodes.UniqueViolation => MapUniqueViolation(exception, entityName),

            PostgresErrorCodes.NotNullViolation =>
                Error.Validation(
                    "database.not_null_violation",
                    $"The {entityName} contains an empty value required by the database.",
                    exception.ColumnName),

            PostgresErrorCodes.StringDataRightTruncation =>
                Error.Validation(
                    "database.value_too_long",
                    $"One of the {entityName} fields exceeds the maximum allowed length.",
                    exception.ColumnName),

            PostgresErrorCodes.CheckViolation =>
                Error.Validation(
                    "database.check_violation",
                    $"The {entityName} violates a database constraint."),

            PostgresErrorCodes.ForeignKeyViolation => MapForeignKeyViolation(exception, entityName),

            _ => SaveFailed(entityName),
        };
    }

    private static Error MapUniqueViolation(PostgresException exception, string entityName) =>
        exception.ConstraintName switch
        {
            "ix_locations_name" => LocationErrors.NameDuplicate("Name"),
            "ix_locations_address" => LocationErrors.AddressDuplicate("Address"),
            "ix_departments_identifier" => DepartmentErrors.IdentifierDuplicate("Identifier"),
            "ix_departments_path" => DepartmentErrors.PathDuplicate(),
            "ix_positions_active_name" => PositionErrors.NameDuplicate("Name"),
            "IX_department_locations_department_id_location_id" =>
                DepartmentErrors.LocationAlreadyLinked(),
            "IX_department_positions_department_id_position_id" =>
                DepartmentErrors.PositionAlreadyLinked(),
            _ => Error.Conflict(
                "database.unique_violation",
                $"A {entityName} with the same unique values already exists."),
        };

    private static Error MapForeignKeyViolation(PostgresException exception, string entityName) =>
        exception.ConstraintName switch
        {
            "fk_department_locations_department_id" => DepartmentErrors.NotFound(),
            "fk_department_locations_location_id" => LocationErrors.NotFound(),
            "fk_department_positions_department_id" => DepartmentErrors.NotFound(),
            "fk_department_positions_position_id" => PositionErrors.NotFound(),
            _ => Error.Conflict(
                "database.foreign_key_violation",
                $"The {entityName} references data that does not exist."),
        };
}
