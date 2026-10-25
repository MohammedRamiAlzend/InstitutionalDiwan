using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Common;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

/// <summary>
/// A page of the documents owned by one department — that department's document register, newest
/// first by default. <paramref name="Search"/> matches the title, the subject summary, and the
/// official number; the remaining parameters narrow the result by status, direction, and type.
/// The department must sit inside the caller's organizational scope.
/// </summary>
public sealed record ListDepartmentDocumentsQuery(
    Guid DepartmentId,
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? SortBy = null,
    bool SortDescending = true,
    int? StatusId = null,
    int? DirectionId = null,
    int? DocumentTypeId = null) : IQuery<PagedResult<DocumentResponse>>;
