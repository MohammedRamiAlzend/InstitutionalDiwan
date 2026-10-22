namespace InstitutionalDiwan.Domain.Common;

/// <summary>
/// One page of rows plus the total number of rows that matched the filter. Repositories return this
/// so the application layer can build a paged response without issuing a second count query.
/// </summary>
public sealed record PagedList<T>(IReadOnlyList<T> Items, int TotalCount);
