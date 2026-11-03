using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Notifications;

/// <summary>One inbox row of the notifications of the calling user.</summary>
public sealed record NotificationResponse(
    Guid RecipientId,
    Guid NotificationId,
    int TypeId,
    string TitleAr,
    string BodyAr,
    int PriorityId,
    Guid? RelatedDocumentId,
    Guid? RelatedMovementId,
    string? ActionUrl,
    bool IsRead,
    DateTime? ReadAt,
    DateTime CreatedAt)
{
    public static NotificationResponse From(NotificationInboxItem item) => new(
        item.RecipientId,
        item.NotificationId,
        item.TypeId,
        item.TitleAr,
        item.BodyAr,
        item.PriorityId,
        item.RelatedDocumentId,
        item.RelatedMovementId,
        item.ActionUrl,
        item.IsRead,
        item.ReadAt,
        item.CreatedAt);
}

/// <summary>The unread notification count of the calling user.</summary>
public sealed record UnreadCountResponse(int Count);

/// <summary>How many notifications a bulk read changed.</summary>
public sealed record MarkedReadResponse(int Count);
