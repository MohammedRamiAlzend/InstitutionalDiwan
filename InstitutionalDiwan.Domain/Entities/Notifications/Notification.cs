using InstitutionalDiwan.Domain.Events;

namespace InstitutionalDiwan.Domain.Entities.Notifications;

public sealed class Notification : AggregateRoot<Guid>
{
    private readonly List<NotificationRecipient> _recipients = new();
    private readonly List<NotificationOutbox> _outboxEntries = new();

    public const int MaxTitleLength = 300;
    public const int MaxBodyLength = 2000;
    public const int MaxActionUrlLength = 500;
    public const int MaxRecipients = 500;

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
        string? actionUrl = null,
        DateTime? utcNow = null)
    {
        if (notificationTypeId <= 0)
            return Result.Failure<Notification>(DomainErrors.Notifications.TypeCodeEmpty);

        if (string.IsNullOrWhiteSpace(titleAr))
            return Result.Failure<Notification>(DomainErrors.Notifications.TitleEmpty);

        if (string.IsNullOrWhiteSpace(bodyAr))
            return Result.Failure<Notification>(DomainErrors.Notifications.BodyEmpty);

        if (titleAr.Trim().Length > MaxTitleLength)
            return Result.Failure<Notification>(DomainErrors.Notifications.TitleTooLong);

        if (bodyAr.Trim().Length > MaxBodyLength)
            return Result.Failure<Notification>(DomainErrors.Notifications.BodyTooLong);

        var actionUrlText = string.IsNullOrWhiteSpace(actionUrl) ? null : actionUrl.Trim();
        if (actionUrlText is not null && !IsValidActionUrl(actionUrlText))
            return Result.Failure<Notification>(DomainErrors.Notifications.ActionUrlInvalid);

        var notification = new Notification
        {
            NotificationId = Guid.NewGuid(),
            NotificationTypeId = notificationTypeId,
            TitleAr = titleAr.Trim(),
            BodyAr = bodyAr.Trim(),
            PriorityId = priorityId > 0 ? priorityId : NotificationPriority.NormalId,
            RelatedDocumentId = relatedDocumentId,
            RelatedMovementId = relatedMovementId,
            ActionUrl = actionUrlText,
            CreatedAt = utcNow ?? DateTime.UtcNow
        };

        notification.RaiseDomainEvent(new NotificationCreatedEvent(
            notification.NotificationId,
            notification.NotificationTypeId,
            notification.RelatedDocumentId));

        return Result.Success(notification);
    }

    /// <summary>
    /// True for a relative in-app path such as <c>/documents/{id}</c>: it starts with a single slash
    /// and carries no scheme, host, backslash, or control character, so it can never leave the app.
    /// </summary>
    public static bool IsValidActionUrl(string actionUrl) =>
        actionUrl.Length <= MaxActionUrlLength &&
        actionUrl.StartsWith('/') &&
        !actionUrl.StartsWith("//", StringComparison.Ordinal) &&
        !actionUrl.Contains('\\') &&
        !actionUrl.Any(char.IsControl);

    public Result AddRecipient(Guid userId, Guid? departmentId = null, DateTime? utcNow = null)
    {
        if (_recipients.Any(r => r.UserId == userId))
            return Result.Failure(DomainErrors.Notifications.RecipientDuplicate);

        if (_recipients.Count >= MaxRecipients)
            return Result.Failure(DomainErrors.Notifications.TooManyRecipients);

        _recipients.Add(NotificationRecipient.Create(NotificationId, userId, departmentId, utcNow));
        return Result.Success();
    }

    public Result EnqueueOutbox(int channelId, string? payloadJson, DateTime? utcNow = null)
    {
        var outboxResult = NotificationOutbox.Create(NotificationId, channelId, payloadJson, utcNow);
        if (outboxResult.IsFailure)
            return outboxResult;

        _outboxEntries.Add(outboxResult.Value);
        return Result.Success();
    }
}
