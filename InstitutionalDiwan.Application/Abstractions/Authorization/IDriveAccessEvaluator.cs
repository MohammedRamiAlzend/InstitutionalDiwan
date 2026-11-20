namespace InstitutionalDiwan.Application.Abstractions.Authorization;

/// <summary>
/// Resolves effective access on Drive items (Module 11) for the ambient caller: ADMIN manages
/// everything, a space owner manages their space, and everyone else gets the strongest explicit
/// grant (user or active department) found walking from the item up to the root, stopping after
/// the first node with broken inheritance. Also derives the owner's department head, who reviews
/// deletion requests. The list-query counterpart lives in the infrastructure access predicate;
/// persistence tests hold the two rules equal.
/// </summary>
public interface IDriveAccessEvaluator
{
    /// <summary>True when the ambient caller is ADMIN or holds DRIVE_MANAGE (manages every space).</summary>
    bool IsAdmin { get; }

    /// <summary>The caller's effective DriveRole id on the item, or 0 when they have no access.</summary>
    Task<int> GetEffectiveRoleAsync(Guid itemId, CancellationToken cancellationToken = default);

    /// <summary>True when the caller's effective role is at least <paramref name="requiredRoleId"/>.</summary>
    Task<bool> CanAccessItemAsync(Guid itemId, int requiredRoleId, CancellationToken cancellationToken = default);

    /// <summary>The caller's primary department plus active assignments, for the SQL browse predicate.</summary>
    Task<IReadOnlyCollection<Guid>> GetCallerDepartmentIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The active user who heads the owner's primary department (ManagerEmployeeId → User.EmployeeId),
    /// or null when there is none — callers then fall back to any ADMIN.
    /// </summary>
    Task<Guid?> GetDeletionApproverAsync(Guid ownerUserId, CancellationToken cancellationToken = default);

    /// <summary>True when <paramref name="headUserId"/> actively heads the primary department of <paramref name="memberUserId"/>.</summary>
    Task<bool> IsDepartmentHeadOfAsync(Guid headUserId, Guid memberUserId, CancellationToken cancellationToken = default);
}