namespace DirectoryService.Contracts.Departments;

public sealed record BulkDepartmentResultDto(
    IReadOnlyList<Guid> SucceededIds,
    IReadOnlyList<BulkDepartmentErrorDto> Errors)
{
    public int SucceededCount => SucceededIds.Count;

    public int FailedCount => Errors.Count;
}
