namespace DirectoryService.Contracts.Search;

public sealed record GlobalSearchResultDto(
    string Type,
    Guid Id,
    string Title,
    string Subtitle,
    bool IsActive,
    Guid? DepartmentId);
