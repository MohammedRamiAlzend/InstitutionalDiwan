using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Movements;

namespace InstitutionalDiwan.Application.Abstractions.Notifications;

/// <summary>
/// Stages a notification (inbox rows plus one IN_APP outbox row) inside the caller's unit of work, so
/// it commits atomically with the business change that caused it. It never saves and never throws:
/// a notification problem must not undo a movement or a registration.
/// </summary>
public interface INotificationPublisher
{
    /// <summary>Announces a movement event (see <c>NotificationType</c> ids) to the right department users.</summary>
    Task PublishMovementAsync(
        int notificationTypeId,
        DocumentMovement movement,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Announces a document event to the document's creator.</summary>
    Task PublishDocumentAsync(
        int notificationTypeId,
        Document document,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Announces a document event to the users of one department, except the actor.</summary>
    Task PublishToDepartmentAsync(
        int notificationTypeId,
        Document document,
        Guid departmentId,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}
