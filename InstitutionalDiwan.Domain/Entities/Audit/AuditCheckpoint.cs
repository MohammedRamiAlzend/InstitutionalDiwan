namespace InstitutionalDiwan.Domain.Entities.Audit;

public sealed class AuditCheckpoint : Entity<Guid>
{
    public Guid CheckpointId
    {
        get => Id;
        private set => Id = value;
    }

    // FK → AuditLog.AuditLogId (InstitutionalDiwan.Domain.Entities.Audit.AuditLog)
    public long FromAuditLogId { get; private set; }
    // FK → AuditLog.AuditLogId (InstitutionalDiwan.Domain.Entities.Audit.AuditLog)
    public long ToAuditLogId { get; private set; }
    public string TerminalHash { get; private set; } = null!;
    public long RecordCount { get; private set; }
    // FK → User.UserId (InstitutionalDiwan.Domain.Entities.Identity.User)
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private AuditCheckpoint()
    {
    }

    public static Result<AuditCheckpoint> Create(
        long fromAuditLogId,
        long toAuditLogId,
        string? terminalHash,
        long recordCount,
        Guid createdByUserId,
        DateTime? utcNow = null)
    {
        if (toAuditLogId < fromAuditLogId || recordCount <= 0)
            return Result.Failure<AuditCheckpoint>(DomainErrors.Audit.InvalidCheckpointRange);

        if (string.IsNullOrWhiteSpace(terminalHash) || terminalHash.Trim().Length != 64)
            return Result.Failure<AuditCheckpoint>(DomainErrors.Audit.HashMismatch);

        var checkpoint = new AuditCheckpoint
        {
            CheckpointId = Guid.NewGuid(),
            FromAuditLogId = fromAuditLogId,
            ToAuditLogId = toAuditLogId,
            TerminalHash = terminalHash.Trim().ToLowerInvariant(),
            RecordCount = recordCount,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = utcNow ?? DateTime.UtcNow
        };

        return Result.Success(checkpoint);
    }
}
