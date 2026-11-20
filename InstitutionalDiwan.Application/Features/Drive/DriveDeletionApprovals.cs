using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Drive;

/// <summary>
/// Shared deletion-approval rules (§3): the owner's department head reviews, with an ADMIN fallback
/// when no active head exists; ADMIN may always review. Recipient resolution mirrors the approver
/// derivation so a request notification reaches exactly who may act on it.
/// </summary>
internal static class DriveDeletionApprovals
{
    public static async Task<bool> CanReviewAsync(
        IDriveAccessEvaluator access, Guid reviewerUserId, Guid ownerUserId, CancellationToken cancellationToken) =>
        access.IsAdmin || await access.IsDepartmentHeadOfAsync(reviewerUserId, ownerUserId, cancellationToken);

    public static async Task<IReadOnlyCollection<Guid>> ResolveApproverRecipientsAsync(
        IDriveAccessEvaluator access,
        IUserRepository users,
        IRoleRepository roles,
        Guid ownerUserId,
        CancellationToken cancellationToken)
    {
        var approver = await access.GetDeletionApproverAsync(ownerUserId, cancellationToken);
        if (approver is { } headUserId)
            return new[] { headUserId };

        var adminRole = await roles.GetByCodeAsync("ADMIN", cancellationToken);
        if (adminRole is null)
            return Array.Empty<Guid>();

        var all = await users.ListAllAsync(cancellationToken);
        return all
            .Where(user => user.IsActive && user.UserRoles.Any(role => role.RoleId == adminRole.RoleId))
            .Select(user => user.UserId)
            .ToList();
    }
}