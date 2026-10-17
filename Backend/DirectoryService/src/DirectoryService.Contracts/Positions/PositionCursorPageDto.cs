namespace DirectoryService.Contracts.Positions;

public sealed record PositionCursorPageDto(
    IReadOnlyList<PositionListItemDto> Items,
    string? NextCursor);
