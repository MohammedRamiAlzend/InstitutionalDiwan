using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Common;

namespace InstitutionalDiwan.Application.Features.Audit;

/// <summary>A page of persisted audit-integrity reports, newest first.</summary>
public sealed record GetAuditIntegrityReportsQuery(
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<AuditIntegrityCheckResponse>>;
