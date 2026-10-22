using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Common;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

/// <summary>
/// A page of documents, newest first by default. <paramref name="Search"/> matches the title, the
/// subject summary, and the official number; the remaining parameters narrow the result by status,
/// owning department, direction, and document type.
/// </summary>
public sealed record ListDocumentsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? SortBy = null,
    bool SortDescending = true,
    int? StatusId = null,
    Guid? OwningDepartmentId = null,
    int? DirectionId = null,
    int? DocumentTypeId = null) : IQuery<PagedResult<DocumentResponse>>;
