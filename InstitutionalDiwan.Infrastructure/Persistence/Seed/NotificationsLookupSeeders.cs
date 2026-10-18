using InstitutionalDiwan.Domain.Entities.Notifications;

namespace InstitutionalDiwan.Infrastructure.Persistence.Seed;

/// <summary>Seeds the notification type lookup (Order 63, every environment).</summary>
public sealed class NotificationTypeSeeder : LookupSeederBase<NotificationType>
{
    public override int Order => 63;

    protected override IReadOnlyList<(int Id, string Desc, string DescAr)> Catalog { get; } = new (int, string, string)[]
    {
        (NotificationType.DocumentCreatedId, "Document created", "إنشاء وثيقة"),
        (NotificationType.DocumentRegisteredId, "Document registered", "تسجيل وثيقة"),
        (NotificationType.MovementSentId, "Movement sent", "إرسال إحالة"),
        (NotificationType.MovementReceivedId, "Movement received", "استلام إحالة"),
        (NotificationType.MovementCompletedId, "Movement completed", "إكمال إحالة"),
        (NotificationType.DocumentArchivedId, "Document archived", "أرشفة وثيقة"),
    };

    protected override NotificationType BuildItem(int id, string desc, string descAr)
    {
        var (title, body, channels) = id switch
        {
            NotificationType.DocumentRegisteredId =>
                ("تم تسجيل وثيقة", "تم تسجيل الوثيقة {Title} بالرقم {Number}.", "IN_APP,EMAIL"),
            NotificationType.MovementSentId =>
                ("إحالة جديدة", "تمت إحالة الوثيقة {Title} إلى دائرتك.", "IN_APP"),
            NotificationType.MovementReceivedId =>
                ("تم استلام الإحالة", "تم استلام الوثيقة {Title}.", "IN_APP"),
            NotificationType.MovementCompletedId =>
                ("اكتملت الإحالة", "اكتملت معالجة الوثيقة {Title}.", "IN_APP"),
            NotificationType.DocumentArchivedId =>
                ("تمت الأرشفة", "تمت أرشفة الوثيقة {Title}.", "IN_APP"),
            _ =>
                ("وثيقة جديدة", "تم إنشاء الوثيقة {Title}.", "IN_APP"),
        };

        return NotificationType.Create(id, desc, descAr, title, body, channels).Value;
    }
}

/// <summary>Seeds the notification priority lookup (Order 64, every environment).</summary>
public sealed class NotificationPrioritySeeder : LookupSeederBase<NotificationPriority>
{
    public override int Order => 64;

    protected override IReadOnlyList<(int Id, string Desc, string DescAr)> Catalog { get; } = new (int, string, string)[]
    {
        (NotificationPriority.NormalId, "Normal", "عادي"),
        (NotificationPriority.HighId, "High", "مرتفع"),
        (NotificationPriority.UrgentId, "Urgent", "عاجل"),
    };

    protected override NotificationPriority BuildItem(int id, string desc, string descAr) =>
        NotificationPriority.Create(id, desc, descAr).Value;
}

/// <summary>Seeds the notification delivery channel lookup (Order 66, every environment).</summary>
public sealed class NotificationChannelSeeder : LookupSeederBase<NotificationChannel>
{
    public override int Order => 66;

    protected override IReadOnlyList<(int Id, string Desc, string DescAr)> Catalog { get; } = new (int, string, string)[]
    {
        (NotificationChannel.InAppId, "In application", "داخل التطبيق"),
        (NotificationChannel.EmailId, "Email", "بريد إلكتروني"),
        (NotificationChannel.SmsId, "SMS", "رسالة نصية"),
    };

    protected override NotificationChannel BuildItem(int id, string desc, string descAr) =>
        NotificationChannel.Create(id, desc, descAr).Value;
}

/// <summary>Seeds the notification outbox status lookup (Order 67, every environment).</summary>
public sealed class NotificationOutboxStatusSeeder : LookupSeederBase<NotificationOutboxStatus>
{
    public override int Order => 67;

    protected override IReadOnlyList<(int Id, string Desc, string DescAr)> Catalog { get; } = new (int, string, string)[]
    {
        (NotificationOutboxStatus.PendingId, "Pending", "قيد الانتظار"),
        (NotificationOutboxStatus.SentId, "Sent", "مُرسل"),
        (NotificationOutboxStatus.FailedId, "Failed", "فشل"),
        (NotificationOutboxStatus.DeadLetterId, "Dead letter", "فشل نهائي"),
    };

    protected override NotificationOutboxStatus BuildItem(int id, string desc, string descAr) =>
        NotificationOutboxStatus.Create(id, desc, descAr).Value;
}
