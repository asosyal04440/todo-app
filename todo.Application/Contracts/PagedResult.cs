namespace TodoApi.Application.Contracts;

public sealed class PagedResult<T>(IReadOnlyList<T> items, int totalCount, PageRequest page)
{
    public IReadOnlyList<T> Items { get; } = items;
    public int TotalCount { get; } = totalCount;
    public int PageNumber { get; } = page.PageNumber;
    public int PageSize { get; } = page.PageSize;
    public int TotalPages { get; } = (int)(((long)totalCount + page.PageSize - 1) / page.PageSize);
}
