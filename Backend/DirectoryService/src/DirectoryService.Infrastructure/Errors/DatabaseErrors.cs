using DirectoryService.Domain.Shared;
using Npgsql;

namespace DirectoryService.Infrastructure.Errors;

public static class DatabaseErrors
{
    public static Error OperationFailed(string operation) =>
        GeneralErrors.Failure($"Failed to {operation}.");

    public static Error Timeout(string operation) =>
        GeneralErrors.Failure($"The database timed out while {operation}.");

    public static Error Unavailable(string operation) =>
        GeneralErrors.Failure($"The database is unavailable. Failed to {operation}.");

    public static Error SaveFailed(string entityName) =>
        GeneralErrors.Failure($"Failed to save the {entityName} to the database.");

    public static Error FromPostgresException(
        PostgresException exception,
        string entityName,
        string? uniqueViolationMessage = null)
    {
        return exception.SqlState switch
        {
            PostgresErrorCodes.UniqueViolation =>
                GeneralErrors.Conflict(
                    uniqueViolationMessage ??
                    $"A {entityName} with the same unique values already exists."),

            PostgresErrorCodes.NotNullViolation =>
                GeneralErrors.Validation(
                    $"The {entityName} contains an empty value required by the database.",
                    exception.ColumnName),

            PostgresErrorCodes.StringDataRightTruncation =>
                GeneralErrors.Validation(
                    $"One of the {entityName} fields exceeds the maximum allowed length.",
                    exception.ColumnName),

            PostgresErrorCodes.CheckViolation =>
                GeneralErrors.Validation($"The {entityName} violates a database constraint."),

            PostgresErrorCodes.ForeignKeyViolation =>
                GeneralErrors.Conflict($"The {entityName} references data that does not exist."),

            _ => SaveFailed(entityName),
        };
    }
}
