namespace InstitutionalDiwan.Infrastructure.BackgroundJobs;

/// <summary>Scheduling and retention windows for the Drive maintenance job (Module 11).</summary>
public sealed class DriveJobsOptions
{
    public const string SectionName = "Drive:Jobs";

    public bool Enabled { get; set; } = true;
    public int StartupDelaySeconds { get; set; } = 90;
    public int IntervalMinutes { get; set; } = 60;

    /// <summary>Trashed items are permanently deleted after this many days.</summary>
    public int TrashRetentionDays { get; set; } = 30;

    /// <summary>Pending uploads older than this are abandoned and their reservation released.</summary>
    public int PendingUploadTimeoutMinutes { get; set; } = 60;

    /// <summary>Open edit sessions idle for this long expire.</summary>
    public int EditSessionIdleMinutes { get; set; } = 120;

    public int BatchSize { get; set; } = 200;
}