using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Audit;

/// <summary>
/// Seals every audit entry added since the previous checkpoint into one immutable anchor. The chain
/// is verified before the anchor is written, so a checkpoint can never legitimize a broken chain.
/// </summary>
public sealed record CreateAuditCheckpointCommand(
    Guid CreatedByUserId,
    long? ToAuditLogId = null) : ICommand<AuditCheckpointResponse>;
