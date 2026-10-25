using System.Text.Json;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Audit;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Audit;

internal sealed class RunAuditIntegrityCheckCommandHandler
    : ICommandHandler<RunAuditIntegrityCheckCommand, AuditIntegrityCheckResponse>
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUserRepository _userRepository;
    private readonly AuditLogAppender _auditLogAppender;
    private readonly IUnitOfWork _unitOfWork;

    public RunAuditIntegrityCheckCommandHandler(
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

    public async Task<Result<AuditIntegrityCheckResponse>> Handle(
        RunAuditIntegrityCheckCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.ExecutedByUserId, cancellationToken);
        if (user is null)
            return Result.Failure<AuditIntegrityCheckResponse>(DomainErrors.Identity.UserNotFound);

        if (!user.IsActive)
            return Result.Failure<AuditIntegrityCheckResponse>(DomainErrors.Identity.UserInactive);

        var latest = await _auditLogRepository.GetLatestAsync(cancellationToken);
        if (latest is null)
            return Result.Failure<AuditIntegrityCheckResponse>(DomainErrors.Audit.NoAuditLogs);

        var startAuditLogId = request.StartAuditLogId ?? 1;
        var endAuditLogId = Math.Min(request.EndAuditLogId ?? latest.AuditLogId, latest.AuditLogId);

        if (startAuditLogId < 1 || endAuditLogId < startAuditLogId)
        {
            return Result.Failure<AuditIntegrityCheckResponse>(
                DomainErrors.Audit.InvalidIntegrityRange);
        }

        var expectedPreviousHash = AuditLog.GenesisHash;
        if (startAuditLogId > 1)
        {
            var previous = await _auditLogRepository.GetByIdAsync(
                startAuditLogId - 1, cancellationToken);

            if (previous is null)
            {
                return Result.Failure<AuditIntegrityCheckResponse>(
                    DomainErrors.Audit.PreviousLogNotFound);
            }

            expectedPreviousHash = previous.EventHash;
        }

        var logs = await _auditLogRepository.ListRangeAsync(
            startAuditLogId, endAuditLogId, cancellationToken);

        if (logs.Count == 0)
            return Result.Failure<AuditIntegrityCheckResponse>(DomainErrors.Audit.NoAuditLogs);

        AuditIntegrityCheck? report = null;
        foreach (var log in logs)
        {
            var verification = log.VerifyIntegrity(expectedPreviousHash);
            if (verification.IsFailure)
            {
                report = AuditIntegrityCheck.RecordFailure(
                    startAuditLogId,
                    endAuditLogId,
                    log.AuditLogId,
                    $"{verification.Error.Code}: {verification.Error.Description}",
                    request.ExecutedByUserId,
                    _unitOfWork.UtcNow).Value;
                break;
            }

            expectedPreviousHash = log.EventHash;
        }

        report ??= AuditIntegrityCheck.RecordPass(
            startAuditLogId,
            endAuditLogId,
            request.ExecutedByUserId,
            _unitOfWork.UtcNow).Value;

        _auditLogRepository.AddIntegrityCheck(report);

        var selfAudit = await _auditLogAppender.AppendAsync(
            "AUDIT",
            AuditActionType.ViewId,
            AuditEntityType.AuditLogId,
            report.CheckId.ToString(),
            request.ExecutedByUserId,
            user.PrimaryDepartmentId,
            newValuesJson: JsonSerializer.Serialize(new
            {
                checkId = report.CheckId,
                startAuditLogId,
                endAuditLogId,
                isValid = report.IsValid,
                firstCorruptedLogId = report.FirstCorruptedLogId
            }),
            cancellationToken: cancellationToken);

        if (selfAudit.IsFailure)
            return Result.Failure<AuditIntegrityCheckResponse>(selfAudit.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return AuditIntegrityCheckResponse.From(report);
    }
}
