using System.Text.Json;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Audit;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Audit;

internal sealed class CreateAuditCheckpointCommandHandler
    : ICommandHandler<CreateAuditCheckpointCommand, AuditCheckpointResponse>
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUserRepository _userRepository;
    private readonly AuditLogAppender _auditLogAppender;
    private readonly IUnitOfWork _unitOfWork;

    public CreateAuditCheckpointCommandHandler(
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

    public async Task<Result<AuditCheckpointResponse>> Handle(
        CreateAuditCheckpointCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.CreatedByUserId, cancellationToken);
        if (user is null)
            return Result.Failure<AuditCheckpointResponse>(DomainErrors.Identity.UserNotFound);

        if (!user.IsActive)
            return Result.Failure<AuditCheckpointResponse>(DomainErrors.Identity.UserInactive);

        var latest = await _auditLogRepository.GetLatestAsync(cancellationToken);
        if (latest is null)
            return Result.Failure<AuditCheckpointResponse>(DomainErrors.Audit.NoAuditLogs);

        var previousCheckpoint = await _auditLogRepository.GetLatestCheckpointAsync(cancellationToken);
        var fromAuditLogId = (previousCheckpoint?.ToAuditLogId ?? 0) + 1;
        var toAuditLogId = Math.Min(request.ToAuditLogId ?? latest.AuditLogId, latest.AuditLogId);

        if (toAuditLogId < fromAuditLogId)
            return Result.Failure<AuditCheckpointResponse>(DomainErrors.Audit.NothingToCheckpoint);

        var logs = await _auditLogRepository.ListRangeAsync(
            fromAuditLogId, toAuditLogId, cancellationToken);

        if (logs.Count == 0)
            return Result.Failure<AuditCheckpointResponse>(DomainErrors.Audit.NothingToCheckpoint);

        var expectedPreviousHash = previousCheckpoint?.TerminalHash ?? AuditLog.GenesisHash;
        foreach (var log in logs)
        {
            var verification = log.VerifyIntegrity(expectedPreviousHash);
            if (verification.IsFailure)
                return Result.Failure<AuditCheckpointResponse>(verification.Error);

            expectedPreviousHash = log.EventHash;
        }

        var checkpointResult = AuditCheckpoint.Create(
            fromAuditLogId,
            toAuditLogId,
            logs[^1].EventHash,
            logs.Count,
            request.CreatedByUserId,
            _unitOfWork.UtcNow);

        if (checkpointResult.IsFailure)
            return Result.Failure<AuditCheckpointResponse>(checkpointResult.Error);

        _auditLogRepository.AddCheckpoint(checkpointResult.Value);

        var selfAudit = await _auditLogAppender.AppendAsync(
            "AUDIT",
            AuditActionType.CreateId,
            AuditEntityType.AuditLogId,
            checkpointResult.Value.CheckpointId.ToString(),
            request.CreatedByUserId,
            user.PrimaryDepartmentId,
            newValuesJson: JsonSerializer.Serialize(new
            {
                checkpointId = checkpointResult.Value.CheckpointId,
                fromAuditLogId,
                toAuditLogId,
                terminalHash = checkpointResult.Value.TerminalHash,
                recordCount = logs.Count
            }),
            cancellationToken: cancellationToken);

        if (selfAudit.IsFailure)
            return Result.Failure<AuditCheckpointResponse>(selfAudit.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return AuditCheckpointResponse.From(checkpointResult.Value);
    }
}
