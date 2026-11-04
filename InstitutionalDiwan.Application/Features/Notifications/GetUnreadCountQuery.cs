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
        CancellationToken cancellationToken)
    {
        var total = await _notificationRepository.CountUnreadAsync(request.UserId, cancellationToken);
        var byType = total == 0
            ? Array.Empty<UnreadTypeCount>()
            : await _notificationRepository.CountUnreadByTypeAsync(request.UserId, cancellationToken);

        return Result.Success(new UnreadCountResponse(
            total,
            byType.Select(row => new UnreadTypeCountResponse(row.TypeId, row.Count)).ToList()));
    }
}
