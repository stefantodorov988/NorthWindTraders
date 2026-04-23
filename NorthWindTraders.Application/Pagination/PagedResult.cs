namespace NorthWindTraders.Application.Pagination;

/// <summary>
/// Generic paged payload for application use and mapping to API contracts. No ASP.NET Core dependencies.
/// </summary>
public sealed record PagedResult<T>(
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    IReadOnlyList<T> Items)
{
    /// <summary>
    /// Creates a result using the same page parameters as the request and a pre-fetched page of items.
    /// </summary>
    public static PagedResult<T> From(PageRequest page, int totalCount, IReadOnlyList<T> items) =>
        new(
            page.PageNumber,
            page.PageSize,
            totalCount,
            CalculateTotalPages(totalCount, page.PageSize),
            items);

    public static int CalculateTotalPages(int totalCount, int pageSize)
    {
        if (totalCount <= 0 || pageSize <= 0)
            return 0;

        return (totalCount + pageSize - 1) / pageSize;
    }
}
