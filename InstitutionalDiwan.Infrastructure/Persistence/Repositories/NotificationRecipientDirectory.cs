using InstitutionalDiwan.Application.Abstractions.Notifications;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Repositories;

internal sealed class NotificationRecipientDirectory : INotificationRecipientDirectory
{
    private readonly DiwanDbContext _context;

    public NotificationRecipientDirectory(DiwanDbContext context) => _context = context;

    public async Task<IReadOnlyList<NotificationAudience>> GetDepartmentUsersAsync(
        Guid departmentId,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default)
    {
        // A user belongs to a department through their primary department or a current assignment.
        var assigned = _context.UserDepartmentAssignments
            .AsNoTracking()
            .Where(assignment =>
                assignment.IsActive &&
                assignment.DepartmentId == departmentId &&
                assignment.StartDate <= asOfUtc &&
                (assignment.EndDate == null || assignment.EndDate > asOfUtc))
            .Select(assignment => assignment.UserId);

        var userIds = await _context.Users
            .AsNoTracking()
            .Where(user =>
                user.IsActive &&
                (user.PrimaryDepartmentId == departmentId || assigned.Contains(user.UserId)))
            .Select(user => user.UserId)
            .ToListAsync(cancellationToken);

        return userIds.Select(userId => new NotificationAudience(userId, departmentId)).ToList();
    }

    public async Task<IReadOnlyList<Guid>> FilterNotifiableAsync(
        Guid documentId,
        IReadOnlyCollection<Guid> userIds,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
            return Array.Empty<Guid>();

        var ids = userIds.ToList();

        var activeIds = await _context.Users
            .AsNoTracking()
            .Where(user => user.IsActive && ids.Contains(user.UserId))
            .Select(user => user.UserId)
            .ToListAsync(cancellationToken);

        var document = await _context.Documents
            .AsNoTracking()
            .Where(candidate => candidate.DocumentId == documentId)
            .Select(candidate => new { candidate.ConfidentialityLevelId, candidate.CreatedByUserId })
            .FirstOrDefaultAsync(cancellationToken);

        if (document is null)
            return Array.Empty<Guid>();

        if (document.ConfidentialityLevelId is not (DocumentConfidentialityLevel.SecretId or DocumentConfidentialityLevel.TopSecretId))
            return activeIds;

        var granted = await _context.DocumentAccessGrants
            .AsNoTracking()
            .Where(grant =>
                grant.DocumentId == documentId &&
                !grant.IsRevoked &&
                (grant.ExpiresAt == null || grant.ExpiresAt > asOfUtc) &&
                ids.Contains(grant.UserId))
            .Select(grant => grant.UserId)
            .ToListAsync(cancellationToken);

        return activeIds
            .Where(userId => userId == document.CreatedByUserId || granted.Contains(userId))
            .ToList();
    }
}
