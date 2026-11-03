namespace InstitutionalDiwan.Application.Abstractions.Notifications;

/// <summary>
/// The realtime alert pushed to a connected user. It is only a hint: the inbox rows in the database
/// stay the source of truth, so a missed push is recovered on the next inbox load.
/// </summary>
public sealed record NotificationPush(
    Guid RecipientId,
    Guid NotificationId,
    int TypeId,
    string TitleAr,
    string BodyAr,
    int PriorityId,
    string? ActionUrl,
    DateTime CreatedAt);

/// <summary>Delivers a <see cref="NotificationPush"/> to the user's open sessions (SignalR in the Server).</summary>
public interface IRealtimeNotifier
{
    Task NotifyUserAsync(Guid userId, NotificationPush push, CancellationToken cancellationToken = default);
}
