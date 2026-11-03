namespace InstitutionalDiwan.Application.Abstractions.Notifications;

/// <summary>A user to notify and the department they are being notified through.</summary>
public sealed record NotificationAudience(Guid UserId, Guid? DepartmentId);

/// <summary>Read-only lookups the notification publisher needs about who may be told what.</summary>
public interface INotificationRecipientDirectory
{
    /// <summary>Active users holding an active, current assignment to the department.</summary>
    Task<IReadOnlyList<NotificationAudience>> GetDepartmentUsersAsync(
        Guid departmentId, DateTime asOfUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Keeps the active users among <paramref name="userIds"/> who may know the document exists. A
    /// SECRET or TOP_SECRET document is only announced to its creator and to users holding a valid
    /// grant; any other document is announced to every candidate, who are operational participants.
    /// </summary>
    Task<IReadOnlyList<Guid>> FilterNotifiableAsync(
        Guid documentId, IReadOnlyCollection<Guid> userIds, DateTime asOfUtc, CancellationToken cancellationToken = default);
}
