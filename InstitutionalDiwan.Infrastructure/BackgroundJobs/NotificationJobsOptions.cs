namespace InstitutionalDiwan.Infrastructure.BackgroundJobs;

/// <summary>Scheduling, retry and retention settings for the notification outbox and retention jobs.</summary>
public sealed class NotificationJobsOptions
{
    public const string SectionName = "NotificationJobs";

    /// <summary>Set to false to disable both notification jobs in an environment.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Delay before the first run, so startup migrations and seeding finish first.</summary>
    public int StartupDelaySeconds { get; init; } = 15;

    /// <summary>How often the outbox is polled for due rows.</summary>
    public int OutboxPollSeconds { get; init; } = 5;

    /// <summary>Outbox rows processed per poll.</summary>
    public int OutboxBatchSize { get; init; } = 50;

    /// <summary>Failures after which a row becomes a dead letter.</summary>
    public int MaxRetries { get; init; } = 5;

    /// <summary>Base of the exponential backoff: the n-th retry waits base * 2^(n-1) seconds.</summary>
    public int RetryBaseSeconds { get; init; } = 30;

    /// <summary>How often retention runs.</summary>
    public int RetentionIntervalHours { get; init; } = 24;

    /// <summary>Read or dismissed inbox rows older than this are deleted.</summary>
    public int RecipientRetentionDays { get; init; } = 90;

    /// <summary>Sent outbox rows older than this are deleted; dead letters are kept.</summary>
    public int SentOutboxRetentionDays { get; init; } = 30;
}
