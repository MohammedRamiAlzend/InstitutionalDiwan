namespace InstitutionalDiwan.Domain.Entities.Audit;

public sealed class AuditIntegrityCheck : Entity<Guid>
{
    public Guid CheckId
    {
        get => Id;
        private set => Id = value;
    }

    // FK → AuditLog.AuditLogId (InstitutionalDiwan.Domain.Entities.Audit.AuditLog)
    public long StartAuditLogId { get; private set; }
    // FK → AuditLog.AuditLogId (InstitutionalDiwan.Domain.Entities.Audit.AuditLog)
    public long EndAuditLogId { get; private set; }
    public bool IsValid { get; private set; }
    // FK → AuditLog.AuditLogId (InstitutionalDiwan.Domain.Entities.Audit.AuditLog)
    public long? FirstCorruptedLogId { get; private set; }
    public string? FailureDetails { get; private set; }
    // FK → User.UserId (InstitutionalDiwan.Domain.Entities.Identity.User)
    public Guid ExecutedByUserId { get; private set; }
    public DateTime ExecutedAtUtc { get; private set; }

    private AuditIntegrityCheck()
    {
    }

    public static Result<AuditIntegrityCheck> RecordPass(
        long startAuditLogId,
        long endAuditLogId,
        Guid executedByUserId,
        DateTime? utcNow = null)
    {
        if (endAuditLogId < startAuditLogId)
            return Result.Failure<AuditIntegrityCheck>(DomainErrors.Audit.InvalidCheckpointRange);

        return Result.Success(new AuditIntegrityCheck
        {
            CheckId = Guid.NewGuid(),
            StartAuditLogId = startAuditLogId,
            EndAuditLogId = endAuditLogId,
            IsValid = true,
            ExecutedByUserId = executedByUserId,
            ExecutedAtUtc = utcNow ?? DateTime.UtcNow
        });
    }

    public static Result<AuditIntegrityCheck> RecordFailure(
        long startAuditLogId,
        long endAuditLogId,
        long firstCorruptedLogId,
        string failureDetails,
        Guid executedByUserId,
        DateTime? utcNow = null)
    {
        if (endAuditLogId < startAuditLogId)
            return Result.Failure<AuditIntegrityCheck>(DomainErrors.Audit.InvalidCheckpointRange);

        return Result.Success(new AuditIntegrityCheck
        {
            CheckId = Guid.NewGuid(),
            StartAuditLogId = startAuditLogId,
            EndAuditLogId = endAuditLogId,
            IsValid = false,
            FirstCorruptedLogId = firstCorruptedLogId,
            FailureDetails = failureDetails,
            ExecutedByUserId = executedByUserId,
            ExecutedAtUtc = utcNow ?? DateTime.UtcNow
        });
    }
}
