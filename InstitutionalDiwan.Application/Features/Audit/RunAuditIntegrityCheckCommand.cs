using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Audit;

/// <summary>
/// Recomputes the SHA-256 chain for a range of audit entries and persists the report. A broken
/// chain is reported as a successful command with <c>isValid = false</c> and the first corrupted id.
/// </summary>
public sealed record RunAuditIntegrityCheckCommand(
    Guid ExecutedByUserId,
    long? StartAuditLogId = null,
    long? EndAuditLogId = null) : ICommand<AuditIntegrityCheckResponse>;
