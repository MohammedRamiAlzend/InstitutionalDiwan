using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace InstitutionalDiwan.Server.Hubs;

/// <summary>
/// Pushes the <c>notificationReceived</c> alert to a signed-in user. The hub accepts no client calls:
/// the database stays the source of truth and the client reloads its inbox on every alert and on every
/// reconnect, so a missed push is never a lost notification.
/// </summary>
[Authorize]
public sealed class NotificationsHub : Hub
{
    public const string Route = "/hubs/notifications";
    public const string ReceivedEvent = "notificationReceived";

    public static string GroupFor(Guid userId) => $"user:{userId:D}";

    public override async Task OnConnectedAsync()
    {
        if (!Guid.TryParse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
        {
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupFor(userId));

        // A connection must not outlive the token that opened it.
        if (long.TryParse(Context.User?.FindFirst("exp")?.Value, out var expiresAtUnix))
        {
            var remaining = DateTimeOffset.FromUnixTimeSeconds(expiresAtUnix) - DateTimeOffset.UtcNow;
            var connection = Context;
            _ = Task.Delay(remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero, connection.ConnectionAborted)
                .ContinueWith(
                    task =>
                    {
                        if (task.IsCompletedSuccessfully)
                            connection.Abort();
                    },
                    TaskScheduler.Default);
        }

        await base.OnConnectedAsync();
    }
}
