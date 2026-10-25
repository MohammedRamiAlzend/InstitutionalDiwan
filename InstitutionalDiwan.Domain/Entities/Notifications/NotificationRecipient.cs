namespace InstitutionalDiwan.Domain.Entities.Notifications;

public sealed class NotificationRecipient : Entity<Guid>
{
    public Guid RecipientId
    {
        get => Id;
        private set => Id = value;
    }

    // FK → Notification.NotificationId (InstitutionalDiwan.Domain.Entities.Notifications.Notification)
    public Guid NotificationId { get; private set; }
    // FK → User.UserId (InstitutionalDiwan.Domain.Entities.Identity.User)
    public Guid UserId { get; private set; }
    // FK → Department.DepartmentId (InstitutionalDiwan.Domain.Entities.Organization.Department)
    public Guid? DepartmentId { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public bool IsDismissed { get; private set; }
    public DateTime DeliveredAt { get; private set; }

    private NotificationRecipient()
    {
    }

    internal static NotificationRecipient Create(Guid notificationId, Guid userId, Guid? departmentId = null, DateTime? utcNow = null)
    {
        return new NotificationRecipient
        {
            RecipientId = Guid.NewGuid(),
            NotificationId = notificationId,
            UserId = userId,
            DepartmentId = departmentId,
            IsRead = false,
            IsDismissed = false,
            DeliveredAt = utcNow ?? DateTime.UtcNow
        };
    }

    public Result MarkAsRead(DateTime? utcNow = null)
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAt = utcNow ?? DateTime.UtcNow;
        }
        return Result.Success();
    }

    public Result Dismiss()
    {
        IsDismissed = true;
        return Result.Success();
    }
}
