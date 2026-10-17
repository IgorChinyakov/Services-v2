namespace DirectoryService.Contracts.Positions;

public sealed record PositionListItemDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAt);
