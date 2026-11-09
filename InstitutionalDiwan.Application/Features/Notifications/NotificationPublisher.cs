using System.Text.Json;
using InstitutionalDiwan.Application.Abstractions.Notifications;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Movements;
using InstitutionalDiwan.Domain.Entities.Notifications;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace InstitutionalDiwan.Application.Features.Notifications;

/// <summary>
/// Builds a <see cref="Notification"/> for a business event and adds it to the current unit of work.
/// Recipient rules: a dispatched or redirected movement goes to its assignee (else the destination
/// department's users); every other movement event goes back to the source department's users plus
/// the movement's creator; document events go to the document's creator. The actor is never notified
/// and recipients who may not know the document exists are dropped.
/// </summary>
internal sealed class NotificationPublisher : INotificationPublisher
{
    private readonly INotificationRepository _notifications;
    private readonly IDocumentRepository _documents;
    private readonly INotificationRecipientDirectory _directory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<NotificationPublisher> _logger;

    public NotificationPublisher(
        INotificationRepository notifications,
        IDocumentRepository documents,
        INotificationRecipientDirectory directory,
        IUnitOfWork unitOfWork,
        ILogger<NotificationPublisher> logger)
    {
        _notifications = notifications;
        _documents = documents;
        _directory = directory;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task PublishMovementAsync(
        int notificationTypeId,
        DocumentMovement movement,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var document = await _documents.GetByIdAsync(movement.DocumentId, cancellationToken);
            if (document is null)
                return;

            var now = _unitOfWork.UtcNow;
            var audience = new Dictionary<Guid, Guid?>();

            if (notificationTypeId is NotificationType.MovementSentId or NotificationType.MovementRedirectedId)
            {
                if (movement.AssignedToUserId is { } assignee)
                {
                    audience[assignee] = movement.DestinationDepartmentId;
                }
                else
                {
                    foreach (var user in await _directory.GetDepartmentUsersAsync(
                                 movement.DestinationDepartmentId, now, cancellationToken))
                    {
                        audience.TryAdd(user.UserId, user.DepartmentId);
                    }
                }
            }
            else
            {
                foreach (var user in await _directory.GetDepartmentUsersAsync(
                             movement.SourceDepartmentId, now, cancellationToken))
                {
                    audience.TryAdd(user.UserId, user.DepartmentId);
                }

                audience.TryAdd(movement.CreatedByUserId, movement.SourceDepartmentId);
            }

            await StageAsync(notificationTypeId, document, movement.MovementId, actorUserId, audience, now, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Staging movement notification {TypeId} failed.", notificationTypeId);
        }
    }

    public async Task PublishDocumentAsync(
        int notificationTypeId,
        Document document,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var audience = new Dictionary<Guid, Guid?> { [document.CreatedByUserId] = document.OwningDepartmentId };
            await StageAsync(notificationTypeId, document, null, actorUserId, audience, _unitOfWork.UtcNow, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Staging document notification {TypeId} failed.", notificationTypeId);
        }
    }

    private async Task StageAsync(
        int notificationTypeId,
        Document document,
        Guid? movementId,
        Guid actorUserId,
        Dictionary<Guid, Guid?> audience,
        DateTime now,
        CancellationToken cancellationToken)
    {
        audience.Remove(actorUserId);
        if (audience.Count == 0)
            return;

        var type = await _notifications.GetTypeAsync(notificationTypeId, cancellationToken);
        if (type is null || !type.IsActive)
            return;

        var notifiable = await _directory.FilterNotifiableAsync(
            document.DocumentId, audience.Keys.ToList(), now, cancellationToken);
        if (notifiable.Count == 0)
            return;

        if (notifiable.Count > Notification.MaxRecipients)
        {
            _logger.LogWarning(
                "Notification {TypeId} for document {DocumentId} had {Count} recipients; truncated to {Max}.",
                notificationTypeId, document.DocumentId, notifiable.Count, Notification.MaxRecipients);
        }

        var title = Truncate(Fill(type.TitleTemplateAr, document), Notification.MaxTitleLength);
        var body = Truncate(Fill(type.BodyTemplateAr, document), Notification.MaxBodyLength);
        var actionUrl = $"/documents/{document.DocumentId}";

        var created = Notification.Create(
            notificationTypeId, title, body, NotificationPriority.NormalId,
            document.DocumentId, movementId, actionUrl, now);
        if (created.IsFailure)
        {
            _logger.LogWarning("Notification {TypeId} was rejected: {Code}.", notificationTypeId, created.Error.Code);
            return;
        }

        var notification = created.Value;
        foreach (var userId in notifiable.Take(Notification.MaxRecipients))
            notification.AddRecipient(userId, audience[userId], now);

        var payload = JsonSerializer.Serialize(
            new
            {
                typeId = notificationTypeId,
                titleAr = title,
                bodyAr = body,
                priorityId = NotificationPriority.NormalId,
                actionUrl,
                createdAt = now,
            },
            PayloadOptions);
        notification.EnqueueOutbox(NotificationChannel.InAppId, payload, now);

        _notifications.Add(notification);
    }

    // Keep Arabic readable (not unicode-escaped) so the payload stays within the outbox column limit.
    private static readonly JsonSerializerOptions PayloadOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];

    private static string Fill(string template, Document document) =>
        template
            .Replace("{Title}", document.DisplayName, StringComparison.Ordinal)
            .Replace("{Number}", document.OfficialNumberText ?? string.Empty, StringComparison.Ordinal);
}
