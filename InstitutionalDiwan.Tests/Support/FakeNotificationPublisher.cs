using InstitutionalDiwan.Application.Abstractions.Notifications;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Movements;

namespace InstitutionalDiwan.Tests.Support;

/// <summary>
/// An <see cref="INotificationPublisher"/> that records what a handler asked to announce instead of
/// staging rows, so handler tests can assert the event type without a recipient directory.
/// </summary>
internal sealed class FakeNotificationPublisher : INotificationPublisher
{
    /// <summary>Shared instance for tests that do not care about notifications.</summary>
    public static FakeNotificationPublisher Instance { get; } = new();

    public List<(int TypeId, Guid DocumentId, Guid? MovementId, Guid ActorUserId)> Published { get; } = new();

    public Task PublishMovementAsync(
        int notificationTypeId,
        DocumentMovement movement,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        Published.Add((notificationTypeId, movement.DocumentId, movement.MovementId, actorUserId));
        return Task.CompletedTask;
    }

    public Task PublishDocumentAsync(
        int notificationTypeId,
        Document document,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        Published.Add((notificationTypeId, document.DocumentId, null, actorUserId));
        return Task.CompletedTask;
    }
}
