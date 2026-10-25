namespace InstitutionalDiwan.Server.Contracts.Audit;

/// <summary>Seals the audit chain up to the latest entry (or an optional explicit id).</summary>
public sealed class CreateAuditCheckpointRequest
{
    /// <summary>Optional last audit id to include. Defaults to the latest entry.</summary>
    /// <example>1024</example>
    public long? ToAuditLogId { get; init; }
}

/// <summary>Runs a hash-chain verification over an optional range; the defaults cover the full chain.</summary>
public sealed class RunAuditIntegrityCheckRequest
{
    /// <summary>Optional first audit id. Defaults to 1 (the genesis entry).</summary>
    /// <example>1</example>
    public long? StartAuditLogId { get; init; }

    /// <summary>Optional last audit id. Defaults to the latest entry.</summary>
    /// <example>2048</example>
    public long? EndAuditLogId { get; init; }
}
