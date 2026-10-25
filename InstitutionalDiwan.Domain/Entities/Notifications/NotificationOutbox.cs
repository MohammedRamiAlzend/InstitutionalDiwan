namespace InstitutionalDiwan.Domain.Entities.Notifications;

public sealed class NotificationOutbox : Entity<Guid>
{
    public Guid OutboxId
    {
        get => Id;
        private set => Id = value;
    }

    // FK → Notification.NotificationId (InstitutionalDiwan.Domain.Entities.Notifications.Notification)
    public Guid NotificationId { get; private set; }
    // FK → NotificationChannel.Id (InstitutionalDiwan.Domain.Entities.Notifications.NotificationChannel)
    public int ChannelId { get; private set; } = NotificationChannel.InAppId;
    public string PayloadJson { get; private set; } = null!;
    // FK → NotificationOutboxStatus.Id (InstitutionalDiwan.Domain.Entities.Notifications.NotificationOutboxStatus)
    public int StatusId { get; private set; } = NotificationOutboxStatus.PendingId;
    public int RetryCount { get; private set; }
    public string? LastError { get; private set; }
    public DateTime? NextRetryAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private NotificationOutbox()
    {
    }

    internal static Result<NotificationOutbox> Create(Guid notificationId, int channelId, string? payloadJson, DateTime? utcNow = null)
    {
        if (channelId <= 0)
            return Result.Failure<NotificationOutbox>(DomainErrors.Notifications.ChannelEmpty);

        var outbox = new NotificationOutbox
        {
            OutboxId = Guid.NewGuid(),
            NotificationId = notificationId,
            ChannelId = channelId,
            PayloadJson = string.IsNullOrWhiteSpace(payloadJson) ? "{}" : payloadJson.Trim(),
            StatusId = NotificationOutboxStatus.PendingId,
            RetryCount = 0,
            CreatedAt = utcNow ?? DateTime.UtcNow
        };

        return Result.Success(outbox);
    }

    public Result MarkProcessed(DateTime? utcNow = null)
    {
        StatusId = NotificationOutboxStatus.SentId;
        ProcessedAt = utcNow ?? DateTime.UtcNow;
        LastError = null;
        return Result.Success();
    }

    public Result RecordFailure(string errorMessage, DateTime? nextRetryAt = null, int maxRetries = 5)
    {
        RetryCount += 1;
        LastError = errorMessage;
        NextRetryAt = nextRetryAt;
        StatusId = RetryCount >= maxRetries ? NotificationOutboxStatus.DeadLetterId : NotificationOutboxStatus.FailedId;
        return Result.Success();
    }
}
