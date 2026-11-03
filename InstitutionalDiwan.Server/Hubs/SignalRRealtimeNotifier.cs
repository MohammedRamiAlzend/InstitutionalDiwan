using InstitutionalDiwan.Application.Abstractions.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace InstitutionalDiwan.Server.Hubs;

/// <summary>Delivers a notification alert to every open session of the recipient through SignalR.</summary>
internal sealed class SignalRRealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<NotificationsHub> _hub;

    public SignalRRealtimeNotifier(IHubContext<NotificationsHub> hub) => _hub = hub;

    public Task NotifyUserAsync(Guid userId, NotificationPush push, CancellationToken cancellationToken = default) =>
        _hub.Clients.Group(NotificationsHub.GroupFor(userId))
            .SendAsync(NotificationsHub.ReceivedEvent, push, cancellationToken);
}
