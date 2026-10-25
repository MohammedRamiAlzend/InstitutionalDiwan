namespace InstitutionalDiwan.Infrastructure.BackgroundJobs;

/// <summary>
/// Scheduling and actor configuration for the audit maintenance jobs. The actor is the account the
/// job records as having created a checkpoint or run an integrity check; when it is omitted the job
/// falls back to the first active user.
/// </summary>
public sealed class AuditJobsOptions
{
    public const string SectionName = "AuditJobs";

    /// <summary>Set to false to disable both audit jobs in an environment.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Explicit account for job-authored audit rows; omit to use the first active user.</summary>
    public Guid? ActorUserId { get; init; }

    /// <summary>Delay before the first run, so startup seeding and migrations finish first.</summary>
    public int StartupDelaySeconds { get; init; } = 60;

    /// <summary>How often the full hash chain is re-verified.</summary>
    public int IntegrityCheckIntervalHours { get; init; } = 24;

    /// <summary>How often a new checkpoint seals the entries added since the previous one.</summary>
    public int CheckpointIntervalHours { get; init; } = 24;

    /// <summary>Offset so the checkpoint does not race the integrity check at the same instant.</summary>
    public int CheckpointOffsetMinutes { get; init; } = 5;
}
