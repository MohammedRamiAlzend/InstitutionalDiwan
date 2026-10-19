namespace InstitutionalDiwan.Domain.Entities.Notifications;

/// <summary>
/// Reference table of notification types (with the Arabic templates used to render them). Replaces
/// the former string <c>Notification.NotificationTypeCode</c>. Seeded by <c>NotificationTypeSeeder</c>.
/// </summary>
public sealed class NotificationType : LookupEntity
{
    public const int DocumentCreatedId = 1;
    public const int DocumentRegisteredId = 2;
    public const int MovementSentId = 3;
    public const int MovementReceivedId = 4;
    public const int MovementCompletedId = 5;
    public const int DocumentArchivedId = 6;

    public string TitleTemplateAr { get; private set; } = null!;
    public string BodyTemplateAr { get; private set; } = null!;

    /// <summary>Comma-separated default delivery channels (kept as a multi-value string).</summary>
    public string DefaultChannelsCsv { get; private set; } = "IN_APP";

    private NotificationType()
    {
    }

    private NotificationType(int id, string desc, string descAr, string titleTemplateAr, string bodyTemplateAr, string defaultChannelsCsv)
        : base(id, desc, descAr)
    {
        TitleTemplateAr = titleTemplateAr.Trim();
        BodyTemplateAr = bodyTemplateAr.Trim();
        DefaultChannelsCsv = defaultChannelsCsv.Trim();
    }

    public static Result<NotificationType> Create(
        int id,
        string? desc,
        string? descAr,
        string? titleTemplateAr,
        string? bodyTemplateAr,
        string defaultChannelsCsv = "IN_APP")
    {
        if (id <= 0)
            return Result.Failure<NotificationType>(DomainErrors.Lookup.InvalidId);

        if (string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(descAr))
            return Result.Failure<NotificationType>(DomainErrors.Notifications.TypeCodeEmpty);

        if (string.IsNullOrWhiteSpace(titleTemplateAr) || string.IsNullOrWhiteSpace(bodyTemplateAr))
            return Result.Failure<NotificationType>(DomainErrors.Notifications.BodyEmpty);

        return Result.Success(new NotificationType(id, desc, descAr, titleTemplateAr, bodyTemplateAr, defaultChannelsCsv));
    }
}

/// <summary>
/// Reference table of notification priorities. Replaces the former string
/// <c>Notification.Priority</c>. Seeded by <c>NotificationPrioritySeeder</c>.
/// </summary>
public sealed class NotificationPriority : LookupEntity
{
    public const int NormalId = 1;
    public const int HighId = 2;
    public const int UrgentId = 3;

    private NotificationPriority()
    {
    }

    private NotificationPriority(int id, string desc, string descAr) : base(id, desc, descAr)
    {
    }

    public static Result<NotificationPriority> Create(int id, string? desc, string? descAr)
    {
        if (id <= 0)
            return Result.Failure<NotificationPriority>(DomainErrors.Lookup.InvalidId);

        if (string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(descAr))
            return Result.Failure<NotificationPriority>(DomainErrors.Lookup.LabelEmpty);

        return Result.Success(new NotificationPriority(id, desc, descAr));
    }
}

/// <summary>
/// Reference table of notification delivery channels. Replaces the former string
/// <c>NotificationOutbox.Channel</c>. Seeded by <c>NotificationChannelSeeder</c>.
/// </summary>
public sealed class NotificationChannel : LookupEntity
{
    public const int InAppId = 1;
    public const int EmailId = 2;
    public const int SmsId = 3;

    private NotificationChannel()
    {
    }

    private NotificationChannel(int id, string desc, string descAr) : base(id, desc, descAr)
    {
    }

    public static Result<NotificationChannel> Create(int id, string? desc, string? descAr)
    {
        if (id <= 0)
            return Result.Failure<NotificationChannel>(DomainErrors.Lookup.InvalidId);

        if (string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(descAr))
            return Result.Failure<NotificationChannel>(DomainErrors.Lookup.LabelEmpty);

        return Result.Success(new NotificationChannel(id, desc, descAr));
    }
}

/// <summary>
/// Reference table of notification outbox delivery statuses. Replaces the former string
/// <c>NotificationOutbox.Status</c>. Seeded by <c>NotificationOutboxStatusSeeder</c>.
/// </summary>
public sealed class NotificationOutboxStatus : LookupEntity
{
    public const int PendingId = 1;
    public const int SentId = 2;
    public const int FailedId = 3;
    public const int DeadLetterId = 4;

    private NotificationOutboxStatus()
    {
    }

    private NotificationOutboxStatus(int id, string desc, string descAr) : base(id, desc, descAr)
    {
    }

    public static Result<NotificationOutboxStatus> Create(int id, string? desc, string? descAr)
    {
        if (id <= 0)
            return Result.Failure<NotificationOutboxStatus>(DomainErrors.Lookup.InvalidId);

        if (string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(descAr))
            return Result.Failure<NotificationOutboxStatus>(DomainErrors.Lookup.LabelEmpty);

        return Result.Success(new NotificationOutboxStatus(id, desc, descAr));
    }
}
