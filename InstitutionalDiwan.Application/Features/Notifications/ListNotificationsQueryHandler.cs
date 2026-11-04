using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Common;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Notifications;

internal sealed class ListNotificationsQueryHandler
    : IQueryHandler<ListNotificationsQuery, PagedResult<NotificationResponse>>
{
    private readonly INotificationRepository _notificationRepository;

    public ListNotificationsQueryHandler(INotificationRepository notificationRepository) =>
        _notificationRepository = notificationRepository;

    public async Task<Result<PagedResult<NotificationResponse>>> Handle(
        ListNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, PageRequest.MaxPageSize);

        var inbox = await _notificationRepository.GetInboxAsync(
            request.UserId,
            request.UnreadOnly,
            request.TypeId,
            new PageRequest(page, pageSize, request.Search, request.SortBy, request.SortDescending),
            cancellationToken);

        return Result.Success(new PagedResult<NotificationResponse>(
            inbox.Items.Select(NotificationResponse.From).ToList(),
            page,
            pageSize,
            inbox.TotalCount));
    }
}
