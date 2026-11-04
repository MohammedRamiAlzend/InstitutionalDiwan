using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Notifications;
using InstitutionalDiwan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Repositories;

internal sealed class NotificationRepository : INotificationRepository
{
    private readonly DiwanDbContext _context;

    public NotificationRepository(DiwanDbContext context) => _context = context;

    public Task<Notification?> GetByIdAsync(Guid notificationId, CancellationToken cancellationToken = default) =>
        _context.Notifications.FirstOrDefaultAsync(
            notification => notification.NotificationId == notificationId, cancellationToken);

    public void Add(Notification notification) => _context.Notifications.Add(notification);

    public async Task<PagedList<NotificationInboxItem>> GetInboxAsync(
        Guid userId,
        bool unreadOnly,
        int? typeId,
        PageRequest page,
        CancellationToken cancellationToken = default)
    {
        var query =
            from recipient in _context.NotificationRecipients.AsNoTracking()
            join notification in _context.Notifications.AsNoTracking()
                on recipient.NotificationId equals notification.NotificationId
            where recipient.UserId == userId && !recipient.IsDismissed
            select new { recipient, notification };

        if (unreadOnly)
            query = query.Where(row => !row.recipient.IsRead);

        if (typeId is { } type)
            query = query.Where(row => row.notification.NotificationTypeId == type);

        if (page.NormalizedSearch is { } search)
        {
            var pattern = $"%{EscapeLike(search)}%";
            query = query.Where(row =>
                EF.Functions.Like(row.notification.TitleAr, pattern, "\\") ||
                EF.Functions.Like(row.notification.BodyAr, pattern, "\\"));
        }

        var total = await query.CountAsync(cancellationToken);

        // No sort key keeps the inbox newest first whatever SortDescending says; the record id is a
        // stable tie-breaker so pages never overlap.
        var ordered = (page.NormalizedSortBy, page.SortDescending) switch
        {
            ("deliveredat", false) => query.OrderBy(row => row.recipient.DeliveredAt),
            ("priority", true) => query.OrderByDescending(row => row.notification.PriorityId),
            ("priority", false) => query.OrderBy(row => row.notification.PriorityId),
            ("type", true) => query.OrderByDescending(row => row.notification.NotificationTypeId),
            ("type", false) => query.OrderBy(row => row.notification.NotificationTypeId),
            ("isread", true) => query.OrderByDescending(row => row.recipient.IsRead),
            ("isread", false) => query.OrderBy(row => row.recipient.IsRead),
            _ => query.OrderByDescending(row => row.recipient.DeliveredAt)
        };

        var items = await ordered
            .ThenBy(row => row.recipient.RecipientId)
            .Skip(page.Skip)
            .Take(page.Take)
            .Select(row => new NotificationInboxItem(
                row.recipient.RecipientId,
                row.notification.NotificationId,
                row.notification.NotificationTypeId,
                row.notification.TitleAr,
                row.notification.BodyAr,
                row.notification.PriorityId,
                row.notification.RelatedDocumentId,
                row.notification.RelatedMovementId,
                row.notification.ActionUrl,
                row.recipient.IsRead,
                row.recipient.ReadAt,
                row.recipient.DeliveredAt))
            .ToListAsync(cancellationToken);

        return new PagedList<NotificationInboxItem>(items, total);
    }

    /// <summary>Escapes the LIKE wildcards so a search for "100%" finds the literal text.</summary>
    private static string EscapeLike(string term) =>
        term.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");

    public async Task<IReadOnlyList<UnreadTypeCount>> CountUnreadByTypeAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await (
            from recipient in _context.NotificationRecipients.AsNoTracking()
            join notification in _context.Notifications.AsNoTracking()
                on recipient.NotificationId equals notification.NotificationId
            where recipient.UserId == userId && !recipient.IsRead && !recipient.IsDismissed
            group notification by notification.NotificationTypeId into byType
            orderby byType.Key
            select new UnreadTypeCount(byType.Key, byType.Count()))
        .ToListAsync(cancellationToken);

    public Task<NotificationType?> GetTypeAsync(int notificationTypeId, CancellationToken cancellationToken = default) =>
        _context.NotificationTypes.AsNoTracking().FirstOrDefaultAsync(
            type => type.Id == notificationTypeId, cancellationToken);

    public Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _context.NotificationRecipients.CountAsync(
            recipient => recipient.UserId == userId && !recipient.IsRead && !recipient.IsDismissed,
            cancellationToken);

    public Task<NotificationRecipient?> GetRecipientAsync(
        Guid recipientId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _context.NotificationRecipients.FirstOrDefaultAsync(
            recipient => recipient.RecipientId == recipientId && recipient.UserId == userId,
            cancellationToken);

    public async Task<IReadOnlyList<NotificationRecipient>> GetRecipientsAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default) =>
        await _context.NotificationRecipients
            .AsNoTracking()
            .Where(recipient => recipient.NotificationId == notificationId)
            .ToListAsync(cancellationToken);

    public Task<NotificationOutbox?> GetOutboxByIdAsync(Guid outboxId, CancellationToken cancellationToken = default) =>
        _context.NotificationOutboxes.FirstOrDefaultAsync(outbox => outbox.OutboxId == outboxId, cancellationToken);

    public Task<int> MarkAllReadAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken = default) =>
        _context.NotificationRecipients
            .Where(recipient => recipient.UserId == userId && !recipient.IsRead && !recipient.IsDismissed)
            .ExecuteUpdateAsync(
                set => set
                    .SetProperty(recipient => recipient.IsRead, true)
                    .SetProperty(recipient => recipient.ReadAt, utcNow),
                cancellationToken);

    public async Task<IReadOnlyList<NotificationOutbox>> GetDueOutboxAsync(
        DateTime utcNow,
        int batchSize,
        CancellationToken cancellationToken = default) =>
        await _context.NotificationOutboxes
            .Where(outbox =>
                (outbox.StatusId == NotificationOutboxStatus.PendingId ||
                 outbox.StatusId == NotificationOutboxStatus.FailedId) &&
                (outbox.NextRetryAt == null || outbox.NextRetryAt <= utcNow))
            .OrderBy(outbox => outbox.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

    public async Task<int> PurgeRecipientsAsync(DateTime deliveredBefore, CancellationToken cancellationToken = default)
    {
        var removed = await _context.NotificationRecipients
            .Where(recipient => recipient.DeliveredAt < deliveredBefore && (recipient.IsRead || recipient.IsDismissed))
            .ExecuteDeleteAsync(cancellationToken);

        // Notifications left without any recipient or pending outbox row carry no information.
        await _context.Notifications
            .Where(notification =>
                notification.CreatedAt < deliveredBefore &&
                !_context.NotificationRecipients.Any(recipient => recipient.NotificationId == notification.NotificationId) &&
                !_context.NotificationOutboxes.Any(outbox => outbox.NotificationId == notification.NotificationId))
            .ExecuteDeleteAsync(cancellationToken);

        return removed;
    }

    public Task<int> PurgeSentOutboxAsync(DateTime processedBefore, CancellationToken cancellationToken = default) =>
        _context.NotificationOutboxes
            .Where(outbox => outbox.StatusId == NotificationOutboxStatus.SentId && outbox.ProcessedAt < processedBefore)
            .ExecuteDeleteAsync(cancellationToken);
}
