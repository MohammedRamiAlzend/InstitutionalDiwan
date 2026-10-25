using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Common;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Audit;

internal sealed class GetAuditIntegrityReportsQueryHandler
    : IQueryHandler<GetAuditIntegrityReportsQuery, PagedResult<AuditIntegrityCheckResponse>>
{
    private readonly IAuditLogRepository _auditLogRepository;

    public GetAuditIntegrityReportsQueryHandler(IAuditLogRepository auditLogRepository) =>
        _auditLogRepository = auditLogRepository;

    public async Task<Result<PagedResult<AuditIntegrityCheckResponse>>> Handle(
        GetAuditIntegrityReportsQuery request,
        CancellationToken cancellationToken)
    {
        var page = new PageRequest(request.Page, request.PageSize);
        var reports = await _auditLogRepository.ListIntegrityChecksAsync(page, cancellationToken);

        return Result.Success(new PagedResult<AuditIntegrityCheckResponse>(
            reports.Items.Select(AuditIntegrityCheckResponse.From).ToList(),
            page.NormalizedPage,
            page.NormalizedPageSize,
            reports.TotalCount));
    }
}
