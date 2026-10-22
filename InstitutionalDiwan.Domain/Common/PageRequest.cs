namespace InstitutionalDiwan.Domain.Common;

/// <summary>
/// Paging, search, and sort input shared by every paged list query. The raw values a caller sends
/// are kept as-is, and the <c>Normalized*</c> members clamp them so no handler or repository has to
/// guard against a zero page, an oversized page size, or a blank search term.
/// </summary>
public sealed record PageRequest(
    int Page = PageRequest.DefaultPage,
    int PageSize = PageRequest.DefaultPageSize,
    string? Search = null,
    string? SortBy = null,
    bool SortDescending = false)
{
    /// <summary>The page a caller gets when they do not ask for one.</summary>
    public const int DefaultPage = 1;

    /// <summary>The page size a caller gets when they do not ask for one.</summary>
    public const int DefaultPageSize = 20;

    /// <summary>The largest page a caller may request, so one request cannot read an entire table.</summary>
    public const int MaxPageSize = 100;

    /// <summary>The requested page, clamped to at least <see cref="DefaultPage"/>.</summary>
    public int NormalizedPage => Page < DefaultPage ? DefaultPage : Page;

    /// <summary>
    /// The requested page size, clamped to <see cref="DefaultPageSize"/>..<see cref="MaxPageSize"/>.
    /// </summary>
    public int NormalizedPageSize => PageSize switch
    {
        < 1 => DefaultPageSize,
        > MaxPageSize => MaxPageSize,
        _ => PageSize
    };

    /// <summary>How many rows to skip before the first row of this page.</summary>
    public int Skip => (NormalizedPage - 1) * NormalizedPageSize;

    /// <summary>How many rows this page may contain.</summary>
    public int Take => NormalizedPageSize;

    /// <summary>The trimmed search term, or <c>null</c> when the caller supplied none.</summary>
    public string? NormalizedSearch =>
        string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();

    /// <summary>The lower-cased sort key, or <c>null</c> when the caller did not choose one.</summary>
    public string? NormalizedSortBy =>
        string.IsNullOrWhiteSpace(SortBy) ? null : SortBy.Trim().ToLowerInvariant();
}
