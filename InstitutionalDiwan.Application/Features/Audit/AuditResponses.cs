using InstitutionalDiwan.Domain.Entities.Audit;

namespace InstitutionalDiwan.Application.Features.Audit;

/// <summary>One append-only audit entry, including the hashes that make the chain verifiable.</summary>
public sealed record AuditLogResponse(
    long AuditLogId,
    DateTime OccurredAtUtc,
    Guid? ActorUserId,
    Guid? ActorDepartmentId,
    string ModuleName,
    int ActionTypeId,
    int EntityTypeId,
    string EntityId,
    Guid? RelatedDocumentId,
    string? OldValuesJson,
    string? NewValuesJson,
    string? IpAddress,
    string? CorrelationId,
    string PreviousHash,
    string EventHash)
{
    public static AuditLogResponse From(AuditLog log) => new(
        log.AuditLogId,
        log.OccurredAtUtc,
        log.ActorUserId,
        log.ActorDepartmentId,
        log.ModuleName,
        log.ActionTypeId,
        log.EntityTypeId,
        log.EntityId,
        log.RelatedDocumentId,
        log.OldValuesJson,
        log.NewValuesJson,
        log.IpAddress,
        log.CorrelationId,
        log.PreviousHash,
        log.EventHash);
}

/// <summary>A sealed anchor over a contiguous range of audit entries.</summary>
public sealed record AuditCheckpointResponse(
    Guid CheckpointId,
    long FromAuditLogId,
    long ToAuditLogId,
    string TerminalHash,
    long RecordCount,
    Guid CreatedByUserId,
    DateTime CreatedAtUtc)
{
    public static AuditCheckpointResponse From(AuditCheckpoint checkpoint) => new(
        checkpoint.CheckpointId,
        checkpoint.FromAuditLogId,
        checkpoint.ToAuditLogId,
        checkpoint.TerminalHash,
        checkpoint.RecordCount,
        checkpoint.CreatedByUserId,
        checkpoint.CreatedAtUtc);
}

/// <summary>The result of walking a range of the audit chain and recomputing every hash.</summary>
public sealed record AuditIntegrityCheckResponse(
    Guid CheckId,
    long StartAuditLogId,
    long EndAuditLogId,
    bool IsValid,
    long? FirstCorruptedLogId,
    string? FailureDetails,
    Guid ExecutedByUserId,
    DateTime ExecutedAtUtc)
{
    public static AuditIntegrityCheckResponse From(AuditIntegrityCheck check) => new(
        check.CheckId,
        check.StartAuditLogId,
        check.EndAuditLogId,
        check.IsValid,
        check.FirstCorruptedLogId,
        check.FailureDetails,
        check.ExecutedByUserId,
        check.ExecutedAtUtc);
}
