using System.Linq.Expressions;
using Api.Application.Contracts.Querying;
using Microsoft.EntityFrameworkCore;

namespace Api.Application.Querying;

public static class QueryableExtensions
{
    public static IQueryable<TSource> WhereIf<TSource>(
        this IQueryable<TSource> source,
        bool condition,
        Expression<Func<TSource, bool>> predicate)
    {
        return condition ? source.Where(predicate) : source;
    }

    public static async Task<PagedResponse<TSource>> ToPagedResponseAsync<TSource>(
        this IQueryable<TSource> source,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var totalCount = await source.CountAsync(cancellationToken);
        var skip = (pageNumber - 1) * pageSize;

        var items = totalCount == 0
            ? []
            : await source
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

        return new PagedResponse<TSource>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
