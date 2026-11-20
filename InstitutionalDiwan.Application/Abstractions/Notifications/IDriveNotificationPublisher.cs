namespace InstitutionalDiwan.Application.Abstractions.Notifications;

/// <summary>
/// Stages a Drive (Module 11) notification to an explicit set of users inside the caller's unit of
/// work, so it commits atomically with the change that caused it. Unlike <see cref="INotificationPublisher"/>
/// (document/movement audiences, confidentiality-filtered), Drive notifies named users — a deletion
/// approver, a requester, a grantee — with no document confidentiality gate. It never saves and never
/// throws: a notification problem must not undo a Drive operation.
/// </summary>
public interface IDriveNotificationPublisher
{
    /// <summary>
    /// Announces a Drive event to the given users (the actor is dropped). The type's Arabic templates
    /// are filled with <c>{Title}</c> (the item name) and <c>{Requester}</c> (the acting user's name).
    /// </summary>
    Task PublishAsync(
        int notificationTypeId,
        IReadOnlyCollection<Guid> recipientUserIds,
        string itemTitle,
        Guid actorUserId,
        string? requesterName = null,
        string? actionUrl = null,
        CancellationToken cancellationToken = default);
}