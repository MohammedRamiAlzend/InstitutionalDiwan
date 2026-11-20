using System.Text.Json;
using InstitutionalDiwan.Application.Abstractions.Notifications;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Notifications;
using InstitutionalDiwan.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace InstitutionalDiwan.Application.Features.Drive;

/// <summary>
/// Builds a Drive <see cref="Notification"/> for named recipients and adds it to the current unit of
/// work. The actor is never notified; an inactive/missing type or an empty audience is a silent no-op.
/// </summary>
internal sealed class DriveNotificationPublisher : IDriveNotificationPublisher
{
    private readonly INotificationRepository _notifications;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DriveNotificationPublisher> _logger;

    public DriveNotificationPublisher(
        INotificationRepository notifications,
        IUnitOfWork unitOfWork,
        ILogger<DriveNotificationPublisher> logger)
    {
        _notifications = notifications;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task PublishAsync(
        int notificationTypeId,
        IReadOnlyCollection<Guid> recipientUserIds,
        string itemTitle,
        Guid actorUserId,
        string? requesterName = null,
        string? actionUrl = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var audience = recipientUserIds.Where(id => id != Guid.Empty && id != actorUserId).Distinct().ToList();
            if (audience.Count == 0)
                return;

            var type = await _notifications.GetTypeAsync(notificationTypeId, cancellationToken);
            if (type is null || !type.IsActive)
                return;

            var now = _unitOfWork.UtcNow;
            var title = Truncate(Fill(type.TitleTemplateAr, itemTitle, requesterName), Notification.MaxTitleLength);
            var body = Truncate(Fill(type.BodyTemplateAr, itemTitle, requesterName), Notification.MaxBodyLength);

            var created = Notification.Create(
                notificationTypeId, title, body, NotificationPriority.NormalId,
                relatedDocumentId: null, relatedMovementId: null, actionUrl, now);
            if (created.IsFailure)
            {
                _logger.LogWarning("Drive notification {TypeId} was rejected: {Code}.", notificationTypeId, created.Error.Code);
                return;
            }

            var notification = created.Value;
            foreach (var userId in audience.Take(Notification.MaxRecipients))
                notification.AddRecipient(userId, null, now);

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
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Staging Drive notification {TypeId} failed.", notificationTypeId);
        }
    }

    // Keep Arabic readable (not unicode-escaped) so the payload stays within the outbox column limit.
    private static readonly JsonSerializerOptions PayloadOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];

    private static string Fill(string template, string itemTitle, string? requesterName) =>
        template
            .Replace("{Title}", itemTitle, StringComparison.Ordinal)
            .Replace("{Requester}", requesterName ?? string.Empty, StringComparison.Ordinal);
}