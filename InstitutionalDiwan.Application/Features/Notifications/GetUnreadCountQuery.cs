using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Notifications;

/// <summary>The unread notification count of the calling user, polled by the bell badge.</summary>
public sealed record GetUnreadCountQuery(Guid UserId) : IQuery<UnreadCountResponse>;

internal sealed class GetUnreadCountQueryHandler : IQueryHandler<GetUnreadCountQuery, UnreadCountResponse>
{
    private readonly INotificationRepository _notificationRepository;

    public GetUnreadCountQueryHandler(INotificationRepository notificationRepository) =>
        _notificationRepository = notificationRepository;

    public async Task<Result<UnreadCountResponse>> Handle(
        GetUnreadCountQuery request,
        CancellationToken cancellationToken) =>
        Result.Success(new UnreadCountResponse(
            await _notificationRepository.CountUnreadAsync(request.UserId, cancellationToken)));
}
