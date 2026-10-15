using System;
using System.Collections.Generic;
using System.Linq;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Events;

namespace InstitutionalDiwan.Domain.Entities.Notifications;

public sealed class Notification : AggregateRoot<Guid>
{
    private readonly List<NotificationRecipient> _recipients = new();
    private readonly List<NotificationOutbox> _outboxEntries = new();

    public Guid NotificationId
    {
        get => Id;
        private set => Id = value;
    }

    public string NotificationTypeCode { get; private set; } = null!;
    public string TitleAr { get; private set; } = null!;
    public string BodyAr { get; private set; } = null!;
    public string Priority { get; private set; } = "NORMAL";
    public Guid? RelatedDocumentId { get; private set; }
    public Guid? RelatedMovementId { get; private set; }
    public string? ActionUrl { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public IReadOnlyCollection<NotificationRecipient> Recipients => _recipients.AsReadOnly();
    public IReadOnlyCollection<NotificationOutbox> OutboxEntries => _outboxEntries.AsReadOnly();

    private Notification()
    {
    }

    public static Result<Notification> Create(
        string? notificationTypeCode,
        string? titleAr,
        string? bodyAr,
        string priority = "NORMAL",
        Guid? relatedDocumentId = null,
        Guid? relatedMovementId = null,
        string? actionUrl = null)
    {
        if (string.IsNullOrWhiteSpace(notificationTypeCode))
            return Result.Failure<Notification>(DomainErrors.Notifications.TypeCodeEmpty);

        if (string.IsNullOrWhiteSpace(titleAr))
            return Result.Failure<Notification>(DomainErrors.Notifications.TitleEmpty);

        if (string.IsNullOrWhiteSpace(bodyAr))
            return Result.Failure<Notification>(DomainErrors.Notifications.BodyEmpty);

        var notification = new Notification
        {
            NotificationId = Guid.NewGuid(),
            NotificationTypeCode = notificationTypeCode.Trim().ToUpperInvariant(),
            TitleAr = titleAr.Trim(),
            BodyAr = bodyAr.Trim(),
            Priority = string.IsNullOrWhiteSpace(priority) ? "NORMAL" : priority.Trim().ToUpperInvariant(),
            RelatedDocumentId = relatedDocumentId,
            RelatedMovementId = relatedMovementId,
            ActionUrl = actionUrl,
            CreatedAt = DateTime.UtcNow
        };

        notification.RaiseDomainEvent(new NotificationCreatedEvent(
            notification.NotificationId,
            notification.NotificationTypeCode,
            notification.RelatedDocumentId));

        return Result.Success(notification);
    }

    public Result AddRecipient(Guid userId, Guid? departmentId = null)
    {
        if (_recipients.Any(r => r.UserId == userId))
            return Result.Failure(DomainErrors.Notifications.RecipientDuplicate);

        _recipients.Add(NotificationRecipient.Create(NotificationId, userId, departmentId));
        return Result.Success();
    }

    public Result EnqueueOutbox(string? channel, string? payloadJson)
    {
        var outboxResult = NotificationOutbox.Create(NotificationId, channel, payloadJson);
        if (outboxResult.IsFailure)
            return outboxResult;

        _outboxEntries.Add(outboxResult.Value);
        return Result.Success();
    }
}
