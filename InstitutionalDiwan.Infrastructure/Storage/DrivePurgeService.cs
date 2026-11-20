using InstitutionalDiwan.Application.Abstractions.Audit;
using InstitutionalDiwan.Application.Abstractions.Drive;
using InstitutionalDiwan.Application.Abstractions.Storage;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Audit;
using InstitutionalDiwan.Domain.Entities.Drive;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace InstitutionalDiwan.Infrastructure.Storage;

/// <summary>
/// Permanent Drive deletion (Module 11): deepest nodes first, S3 objects before rows, quota released
/// per file, one audit row per tree. A failed S3 delete throws so the surrounding transaction rolls
/// back and the next attempt retries — no object is orphaned and no row vanishes while its bytes remain.
/// </summary>
internal sealed class DrivePurgeService : IDrivePurgeService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly DiwanDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDriveSpaceRepository _spaces;
    private readonly IDriveFileVersionRepository _versions;
    private readonly IDriveStorageService _storage;
    private readonly IAuditActionRecorder _recorder;

    public DrivePurgeService(
        DiwanDbContext context,
        IUnitOfWork unitOfWork,
        IDriveSpaceRepository spaces,
        IDriveFileVersionRepository versions,
        IDriveStorageService storage,
        IAuditActionRecorder recorder)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _spaces = spaces;
        _versions = versions;
        _storage = storage;
        _recorder = recorder;
    }

    public async Task PurgeTreeAsync(Guid rootItemId, Guid? actorUserId, string reason, CancellationToken cancellationToken = default)
    {
        var root = await _context.DriveItems.FirstOrDefaultAsync(item => item.ItemId == rootItemId, cancellationToken);
        if (root is null)
            return;

        var tree = new List<DriveItem> { root };
        for (var index = 0; index < tree.Count; index++)
        {
            var children = await _context.DriveItems
                .Where(item => item.ParentId == tree[index].ItemId)
                .ToListAsync(cancellationToken);
            tree.AddRange(children);
        }

        long releasedBytes = 0;
        for (var index = tree.Count - 1; index >= 0; index--)
        {
            var item = tree[index];

            // Sessions hold a Restrict FK to versions, so they go first. Remove them through the
            // change tracker rather than ExecuteDelete: a session already tracked in this context
            // must be marked Deleted, otherwise it lingers as a live dependent and RemoveRange on
            // the versions throws on the severed required relationship.
            var sessions = await _context.DriveEditSessions
                .Where(session => session.FileItemId == item.ItemId)
                .ToListAsync(cancellationToken);
            if (sessions.Count > 0)
            {
                _context.DriveEditSessions.RemoveRange(sessions);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            var versions = await _versions.ListByItemAsync(item.ItemId, cancellationToken);
            long itemBytes = 0;
            foreach (var version in versions)
            {
                await _storage.DeleteAsync(version.ObjectKey, cancellationToken);
                if (version.IsCommitted)
                    itemBytes += version.SizeBytes;
            }

            if (itemBytes > 0 && item.IsFile)
            {
                var space = await _spaces.GetByIdAsync(item.SpaceId, cancellationToken);
                if (space is not null && itemBytes <= space.UsedBytes)
                    space.ReleaseUsage(itemBytes);
            }
            releasedBytes += itemBytes;

            // CurrentVersionId -> version is Restrict while version -> item is Cascade, so deleting
            // both in one batch is a circular dependency. Sever the reference and save it first.
            if (item.CurrentVersionId is not null)
            {
                _context.Entry(item).Property(x => x.CurrentVersionId).CurrentValue = null;
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Permissions, stars and deletion requests cascade from the item row.
            _context.DriveFileVersions.RemoveRange(versions);
            _context.DriveItems.Remove(item);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        await _recorder.RecordAsync(
            new AuditActionRecord(
                "DRIVE",
                AuditActionType.ExecuteId,
                AuditEntityType.DriveItemId,
                rootItemId.ToString(),
                actorUserId,
                ActorDepartmentId: null,
                JsonSerializer.Serialize(new { reason, items = tree.Count, bytes = releasedBytes }, JsonOptions)),
            cancellationToken);
    }
}