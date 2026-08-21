using System.Collections;
using System.Text.Json.Serialization;

namespace DirectoryService.Contracts.Common;

public sealed class PagedList<T>
{
    [JsonConstructor]
    private PagedList(
        IReadOnlyList<T> items,
        int page,
        int pageSize,
        long totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public IReadOnlyList<T> Items { get; }

    public int Page { get; }

    public int PageSize { get; }

    public long TotalCount { get; }

    public int TotalPages =>
        TotalCount == 0
            ? 0
            : (int)Math.Ceiling((double)TotalCount / PageSize);

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;

    public static PagedList<T> Create(
        IReadOnlyList<T> items,
        int page,
        int pageSize,
        long totalCount)
    {
        return new PagedList<T>(
            items,
            page,
            pageSize,
            totalCount);
    }

    public static PagedList<T> Empty(int page, int pageSize)
    {
        return new PagedList<T>([], page, pageSize, 0);
    }
}
