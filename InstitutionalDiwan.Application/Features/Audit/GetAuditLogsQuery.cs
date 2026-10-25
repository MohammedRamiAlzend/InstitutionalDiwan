using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Common;

namespace InstitutionalDiwan.Application.Features.Audit;

/// <summary>
/// A page of audit entries. Every call is itself audited as a sensitive read: the caller, filters,
/// correlation id, and source address are appended to the chain after the page is read.
/// </summary>
public sealed record GetAuditLogsQuery(
    Guid UserId,
    int Page = 1,
    int PageSize = 20,
    string? ModuleName = null,
    int? ActionTypeId = null,
    int? EntityTypeId = null,
    string? EntityId = null,
    Guid? ActorUserId = null,
    Guid? ActorDepartmentId = null,
    Guid? RelatedDocumentId = null,
    string? CorrelationId = null,
    string? IpAddress = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    string? Search = null,
    string? SortBy = null,
    bool SortDescending = false) : IQuery<PagedResult<AuditLogResponse>>;
