using Microsoft.EntityFrameworkCore;
using TodoApi.Application.Contracts;

namespace TodoApi.Infrastructure.Persistence;

internal static class ReadQueryPaging
{
    public static async Task<PagedResult<T>> ReadPageAsync<T>(
        IQueryable<T> orderedQuery, PageRequest page, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(page);
        int total = await orderedQuery.CountAsync(cancellationToken);
        List<T> items = await orderedQuery.Skip(page.Offset).Take(page.PageSize).ToListAsync(cancellationToken);
        return new PagedResult<T>(items, total, page);
    }
}
