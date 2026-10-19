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

    // FK → NotificationType.Id (InstitutionalDiwan.Domain.Entities.Notifications.NotificationType)
    public int NotificationTypeId { get; private set; }
    public string TitleAr { get; private set; } = null!;
    public string BodyAr { get; private set; } = null!;
    // FK → NotificationPriority.Id (InstitutionalDiwan.Domain.Entities.Notifications.NotificationPriority)
    public int PriorityId { get; private set; } = NotificationPriority.NormalId;
    // FK → Document.DocumentId (InstitutionalDiwan.Domain.Entities.Correspondence.Document)
    public Guid? RelatedDocumentId { get; private set; }
    // FK → DocumentMovement.MovementId (InstitutionalDiwan.Domain.Entities.Movements.DocumentMovement)
    public Guid? RelatedMovementId { get; private set; }
    public string? ActionUrl { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public IReadOnlyCollection<NotificationRecipient> Recipients => _recipients.AsReadOnly();
    public IReadOnlyCollection<NotificationOutbox> OutboxEntries => _outboxEntries.AsReadOnly();

    private Notification()
    {
    }

    public static Result<Notification> Create(
        int notificationTypeId,
        string? titleAr,
        string? bodyAr,
        int priorityId = NotificationPriority.NormalId,
        Guid? relatedDocumentId = null,
        Guid? relatedMovementId = null,
        string? actionUrl = null)
    {
        if (notificationTypeId <= 0)
            return Result.Failure<Notification>(DomainErrors.Notifications.TypeCodeEmpty);

        if (string.IsNullOrWhiteSpace(titleAr))
            return Result.Failure<Notification>(DomainErrors.Notifications.TitleEmpty);

        if (string.IsNullOrWhiteSpace(bodyAr))
            return Result.Failure<Notification>(DomainErrors.Notifications.BodyEmpty);

        var notification = new Notification
        {
            NotificationId = Guid.NewGuid(),
            NotificationTypeId = notificationTypeId,
            TitleAr = titleAr.Trim(),
            BodyAr = bodyAr.Trim(),
            PriorityId = priorityId > 0 ? priorityId : NotificationPriority.NormalId,
            RelatedDocumentId = relatedDocumentId,
            RelatedMovementId = relatedMovementId,
            ActionUrl = actionUrl,
            CreatedAt = DateTime.UtcNow
        };

        notification.RaiseDomainEvent(new NotificationCreatedEvent(
            notification.NotificationId,
            notification.NotificationTypeId,
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

    public Result EnqueueOutbox(int channelId, string? payloadJson)
    {
        var outboxResult = NotificationOutbox.Create(NotificationId, channelId, payloadJson);
        if (outboxResult.IsFailure)
            return outboxResult;

        _outboxEntries.Add(outboxResult.Value);
        return Result.Success();
    }
}
