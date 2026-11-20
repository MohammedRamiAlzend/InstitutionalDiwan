namespace InstitutionalDiwan.Application.Abstractions.Drive;

/// <summary>
/// Permanently deletes a trashed Drive item tree (Module 11, §7.3): S3 objects first, then rows
/// (versions/permissions/stars/requests cascade), then the space usage counter, with one audit row.
/// Shared by the manual purge command and the retention cleanup job. <paramref name="actorUserId"/>
/// null means the system actor. A storage failure throws so the caller aborts and nothing is orphaned.
/// </summary>
public interface IDrivePurgeService
{
    Task PurgeTreeAsync(
        Guid rootItemId,
        Guid? actorUserId,
        string reason,
        CancellationToken cancellationToken = default);
}