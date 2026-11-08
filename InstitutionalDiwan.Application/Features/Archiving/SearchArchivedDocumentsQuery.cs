using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Common;

namespace InstitutionalDiwan.Application.Features.Archiving;

/// <summary>
/// A page of the archived documents the caller may view, most recently archived first by default.
/// <paramref name="Search"/> matches the title, the subject summary, and the official number.
/// </summary>
public sealed record SearchArchivedDocumentsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? SortBy = null,
    bool SortDescending = true,
    Guid? OwningDepartmentId = null,
    int? DocumentTypeId = null,
    int? DirectionId = null,
    int? ArchiveLocationId = null,
    DateTime? ArchivedFromUtc = null,
    DateTime? ArchivedToUtc = null,
    IReadOnlyList<string>? FieldFilters = null) : IQuery<PagedResult<ArchivedDocumentResponse>>;
