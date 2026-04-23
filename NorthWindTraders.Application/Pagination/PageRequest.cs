namespace NorthWindTraders.Application.Pagination;

/// <summary>
/// Paging input for use cases and data access. Not tied to HTTP or ASP.NET Core.
/// </summary>
public readonly record struct PageRequest(int PageNumber, int PageSize)
{
    public const int DefaultMaxPageSize = 100;

    /// <summary>Offset for Skip in database queries.</summary>
    public int Skip => (PageNumber - 1) * PageSize;

    /// <summary>
    /// Safe bounds: page at least 1, page size between 1 and <paramref name="maxPageSize"/>.
    /// </summary>
    public static PageRequest Normalize(int pageNumber, int pageSize, int maxPageSize = DefaultMaxPageSize) =>
        new(Math.Max(1, pageNumber), Math.Clamp(pageSize, 1, maxPageSize));
}
