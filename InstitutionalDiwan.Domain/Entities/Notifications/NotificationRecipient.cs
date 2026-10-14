using System;
using InstitutionalDiwan.Domain.Common;

namespace InstitutionalDiwan.Domain.Entities.Notifications;

public sealed class NotificationRecipient : Entity<Guid>
{
    public Guid RecipientId
    {
        get => Id;
        private set => Id = value;
    }

    public Guid NotificationId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public bool IsDismissed { get; private set; }
    public DateTime DeliveredAt { get; private set; }

    private NotificationRecipient()
    {
    }

    internal static NotificationRecipient Create(Guid notificationId, Guid userId, Guid? departmentId = null)
    {
        return new NotificationRecipient
        {
            RecipientId = Guid.NewGuid(),
            NotificationId = notificationId,
            UserId = userId,
            DepartmentId = departmentId,
            IsRead = false,
            IsDismissed = false,
            DeliveredAt = DateTime.UtcNow
        };
    }

    public Result MarkAsRead()
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAt = DateTime.UtcNow;
        }
        return Result.Success();
    }

    public Result Dismiss()
    {
        IsDismissed = true;
        return Result.Success();
    }
}
