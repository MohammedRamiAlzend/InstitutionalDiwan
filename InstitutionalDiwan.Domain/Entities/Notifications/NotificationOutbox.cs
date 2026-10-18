using System;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;

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
    public string Channel { get; private set; } = "IN_APP";
    public string PayloadJson { get; private set; } = null!;
    public string Status { get; private set; } = "PENDING";
    public int RetryCount { get; private set; }
    public string? LastError { get; private set; }
    public DateTime? NextRetryAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private NotificationOutbox()
    {
    }

    internal static Result<NotificationOutbox> Create(Guid notificationId, string? channel, string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(channel))
            return Result.Failure<NotificationOutbox>(DomainErrors.Notifications.ChannelEmpty);

        var outbox = new NotificationOutbox
        {
            OutboxId = Guid.NewGuid(),
            NotificationId = notificationId,
            Channel = channel.Trim().ToUpperInvariant(),
            PayloadJson = string.IsNullOrWhiteSpace(payloadJson) ? "{}" : payloadJson.Trim(),
            Status = "PENDING",
            RetryCount = 0,
            CreatedAt = DateTime.UtcNow
        };

        return Result.Success(outbox);
    }

    public Result MarkProcessed()
    {
        Status = "SENT";
        ProcessedAt = DateTime.UtcNow;
        LastError = null;
        return Result.Success();
    }

    public Result RecordFailure(string errorMessage, DateTime? nextRetryAt = null, int maxRetries = 5)
    {
        RetryCount += 1;
        LastError = errorMessage;
        NextRetryAt = nextRetryAt;
        Status = RetryCount >= maxRetries ? "DEAD_LETTER" : "FAILED";
        return Result.Success();
    }
}
