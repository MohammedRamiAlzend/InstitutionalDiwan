using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Common;

namespace InstitutionalDiwan.Application.Features.Notifications;

/// <summary>The own inbox of the calling user, newest first; dismissed rows are never listed.</summary>
public sealed record ListNotificationsQuery(
    Guid UserId,
    bool UnreadOnly = false,
    int? TypeId = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<NotificationResponse>>;
