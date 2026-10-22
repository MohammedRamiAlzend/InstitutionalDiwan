namespace InstitutionalDiwan.Application.Common;

/// <summary>
/// One page of results plus the counters a client needs to render a pager. This is the wire shape
/// every paged list endpoint returns, so a table does not have to guess how many rows exist.
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyCollection<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    /// <summary>Number of pages, rounded up; zero when the filter matched nothing.</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (TotalCount + PageSize - 1) / PageSize;

    /// <summary>True when a page before this one exists.</summary>
    public bool HasPrevious => Page > 1;

    /// <summary>True when a page after this one exists.</summary>
    public bool HasNext => Page < TotalPages;
}
