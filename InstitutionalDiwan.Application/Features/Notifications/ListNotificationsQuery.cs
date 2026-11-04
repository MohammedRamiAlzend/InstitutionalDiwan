using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Common;

namespace InstitutionalDiwan.Application.Features.Notifications;

/// <summary>
/// The own inbox of the calling user; dismissed rows are never listed. Newest first unless
/// <paramref name="SortBy"/> names another column. <paramref name="Search"/> matches the Arabic
/// title and body.
/// </summary>
public sealed record ListNotificationsQuery(
    Guid UserId,
    bool UnreadOnly = false,
    int? TypeId = null,
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? SortBy = null,
    bool SortDescending = false) : IQuery<PagedResult<NotificationResponse>>;
