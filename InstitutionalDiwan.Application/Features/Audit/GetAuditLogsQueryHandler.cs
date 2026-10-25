using System.Text.Json;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Common;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Audit;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Audit;

internal sealed class GetAuditLogsQueryHandler
    : IQueryHandler<GetAuditLogsQuery, PagedResult<AuditLogResponse>>
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUserRepository _userRepository;
    private readonly AuditLogAppender _auditLogAppender;
    private readonly IUnitOfWork _unitOfWork;

    public GetAuditLogsQueryHandler(
        IAuditLogRepository auditLogRepository,
        IUserRepository userRepository,
        AuditLogAppender auditLogAppender,
        IUnitOfWork unitOfWork)
    {
        _auditLogRepository = auditLogRepository;
        _userRepository = userRepository;
        _auditLogAppender = auditLogAppender;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PagedResult<AuditLogResponse>>> Handle(
        GetAuditLogsQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<PagedResult<AuditLogResponse>>(DomainErrors.Identity.UserNotFound);

        if (!user.IsActive)
            return Result.Failure<PagedResult<AuditLogResponse>>(DomainErrors.Identity.UserInactive);

        if (request.FromUtc is { } from && request.ToUtc is { } to && to < from)
        {
            return Result.Failure<PagedResult<AuditLogResponse>>(
                DomainErrors.Audit.InvalidIntegrityRange);
        }

        var filter = new AuditLogFilter(
            request.ModuleName,
            request.ActionTypeId,
            request.EntityTypeId,
            request.EntityId,
            request.ActorUserId,
            request.ActorDepartmentId,
            request.RelatedDocumentId,
            request.CorrelationId,
            request.IpAddress,
            request.FromUtc,
            request.ToUtc,
            request.Search);

        var page = new PageRequest(
            request.Page,
            request.PageSize,
            request.Search,
            request.SortBy,
            request.SortDescending);

        var logs = await _auditLogRepository.SearchAsync(filter, page, cancellationToken);

        // Reading the audit trail is itself a sensitive action. Record who looked, with which
        // filters, and from where — in the same unit of work as the read.
        var selfAudit = await _auditLogAppender.AppendAsync(
            "AUDIT",
            AuditActionType.ViewId,
            AuditEntityType.AuditLogId,
            "AUDIT_LOGS",
            request.UserId,
            user.PrimaryDepartmentId,
            request.RelatedDocumentId,
            newValuesJson: JsonSerializer.Serialize(new
            {
                action = "search",
                filter = new
                {
                    request.ModuleName,
                    request.ActionTypeId,
                    request.EntityTypeId,
                    request.EntityId,
                    request.ActorUserId,
                    request.ActorDepartmentId,
                    request.RelatedDocumentId,
                    request.CorrelationId,
                    request.IpAddress,
                    request.FromUtc,
                    request.ToUtc,
                    request.Search
                }
            }),
            cancellationToken: cancellationToken);

        if (selfAudit.IsFailure)
            return Result.Failure<PagedResult<AuditLogResponse>>(selfAudit.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new PagedResult<AuditLogResponse>(
            logs.Items.Select(AuditLogResponse.From).ToList(),
            page.NormalizedPage,
            page.NormalizedPageSize,
            logs.TotalCount));
    }
}
