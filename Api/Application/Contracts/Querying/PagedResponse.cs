namespace Api.Application.Contracts.Querying;

public sealed class PagedResponse<TItem>
{
    public required IReadOnlyList<TItem> Items { get; init; }

    public required int PageNumber { get; init; }

    public required int PageSize { get; init; }

    public required int TotalCount { get; init; }

    public int TotalPages => TotalCount == 0
        ? 0
        : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
