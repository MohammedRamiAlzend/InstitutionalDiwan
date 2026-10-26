namespace InstitutionalDiwan.Application.Abstractions.Audit;

/// <summary>One action-level audit entry: what the caller did, independent of entity changes.</summary>
public sealed record AuditActionRecord(
    string ModuleName,
    int ActionTypeId,
    int EntityTypeId,
    string EntityId,
    Guid? ActorUserId,
    Guid? ActorDepartmentId,
    string? NewValuesJson);

/// <summary>
/// Appends an action entry for a completed user operation. Implementations must save in their own
/// unit of work: a failed command may still be tracked in the request context, and flushing it
/// while recording the failure would persist changes the command already abandoned.
/// </summary>
public interface IAuditActionRecorder
{
    Task RecordAsync(AuditActionRecord record, CancellationToken cancellationToken = default);
}
