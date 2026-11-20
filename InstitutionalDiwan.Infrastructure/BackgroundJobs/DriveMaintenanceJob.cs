using InstitutionalDiwan.Application.Abstractions.Audit;
using InstitutionalDiwan.Application.Abstractions.Drive;
using InstitutionalDiwan.Application.Abstractions.Storage;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Audit;
using InstitutionalDiwan.Domain.Entities.Drive;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Infrastructure.Persistence;
using InstitutionalDiwan.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace InstitutionalDiwan.Infrastructure.BackgroundJobs;

/// <summary>
/// Drive maintenance (Module 11): abandons expired pending uploads, expires idle edit sessions,
/// and permanently deletes trash past its retention window — S3 objects first, then rows, then
/// the space usage counter, with one system-actor audit row per purged tree. A failed S3 delete
/// aborts that tree's purge so no object is orphaned; the next run retries.
/// </summary>
internal sealed class DriveMaintenanceJob : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<DriveJobsOptions> _options;
    private readonly IOptions<DriveStorageOptions> _storageOptions;
    private readonly ILogger<DriveMaintenanceJob> _logger;

    public DriveMaintenanceJob(
        IServiceScopeFactory scopeFactory,
        IOptions<DriveJobsOptions> options,
        IOptions<DriveStorageOptions> storageOptions,
        ILogger<DriveMaintenanceJob> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _storageOptions = storageOptions;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _options.Value;
        if (!options.Enabled)
        {
            _logger.LogInformation("DriveMaintenanceJob is disabled by configuration.");
            return;
        }

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(Math.Max(0, options.StartupDelaySeconds)), stoppingToken);
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(Math.Max(1, options.IntervalMinutes)));

            do
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    await RunOnceAsync(scope.ServiceProvider, options, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "DriveMaintenanceJob failed; the next run will retry.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task RunOnceAsync(IServiceProvider services, DriveJobsOptions options, CancellationToken cancellationToken)
    {
        var context = services.GetRequiredService<DiwanDbContext>();
        var unitOfWork = services.GetRequiredService<IUnitOfWork>();
        var versionRepository = services.GetRequiredService<IDriveFileVersionRepository>();
        var sessionRepository = services.GetRequiredService<IDriveEditSessionRepository>();
        var itemRepository = services.GetRequiredService<IDriveItemRepository>();
        var storage = services.GetRequiredService<IDriveStorageService>();
        var now = DateTime.UtcNow;
        var batchSize = Math.Max(1, options.BatchSize);

        var pending = await versionRepository.ListPendingOlderThanAsync(
            now.AddMinutes(-Math.Max(1, options.PendingUploadTimeoutMinutes)), batchSize, cancellationToken);
        foreach (var version in pending)
        {
            version.Abandon();
            await TryDeleteObjectAsync(storage, version.ObjectKey, cancellationToken);
        }

        var idle = await sessionRepository.ListIdleOpenAsync(
            now.AddMinutes(-Math.Max(1, options.EditSessionIdleMinutes)), batchSize, cancellationToken);
        foreach (var session in idle)
            session.Expire(now);

        if (pending.Count > 0 || idle.Count > 0)
            await unitOfWork.SaveChangesAsync(cancellationToken);

        if (!_storageOptions.Value.IsConfigured)
        {
            if (pending.Count > 0 || idle.Count > 0)
                _logger.LogInformation("Drive maintenance abandoned {Pending} uploads and expired {Sessions} sessions.", pending.Count, idle.Count);
            return;
        }

        var cutoff = now.AddDays(-Math.Max(1, options.TrashRetentionDays));
        var recorder = services.GetRequiredService<IAuditActionRecorder>();
        var purgeService = services.GetRequiredService<IDrivePurgeService>();

        var expiredRoots = await itemRepository.ListTrashExpiredAsync(cutoff, batchSize, cancellationToken);
        foreach (var root in expiredRoots)
            await purgeService.PurgeTreeAsync(root.ItemId, null, "TrashRetention", cancellationToken);

        var expiredSpaces = await context.DriveSpaces
            .Where(space => space.TrashedAt != null && space.TrashedAt < cutoff)
            .OrderBy(space => space.TrashedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
        foreach (var space in expiredSpaces)
        {
            var roots = await context.DriveItems
                .Where(item => item.SpaceId == space.SpaceId && item.ParentId == null)
                .ToListAsync(cancellationToken);
            foreach (var root in roots)
                await purgeService.PurgeTreeAsync(root.ItemId, null, "TrashRetention", cancellationToken);

            context.DriveSpaces.Remove(space);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await RecordPurgeAsync(recorder, space.SpaceId, AuditEntityType.DriveSpaceId, 0, 0, cancellationToken);
        }

        _logger.LogInformation(
            "Drive maintenance abandoned {Pending} uploads, expired {Sessions} sessions, purged {Trees} trashed trees and {Spaces} trashed spaces.",
            pending.Count, idle.Count, expiredRoots.Count, expiredSpaces.Count);
    }

    /// <summary>Appends the system-actor audit row for a purged space.</summary>
    private static Task RecordPurgeAsync(
        IAuditActionRecorder recorder,
        Guid entityId,
        int entityTypeId,
        int itemCount,
        long releasedBytes,
        CancellationToken cancellationToken) =>
        recorder.RecordAsync(
            new AuditActionRecord(
                "DRIVE",
                AuditActionType.ExecuteId,
                entityTypeId,
                entityId.ToString(),
                ActorUserId: null,
                ActorDepartmentId: null,
                JsonSerializer.Serialize(new { reason = "TrashRetention", items = itemCount, bytes = releasedBytes }, JsonOptions)),
            cancellationToken);

    private async Task TryDeleteObjectAsync(IDriveStorageService storage, string objectKey, CancellationToken cancellationToken)
    {
        try
        {
            await storage.DeleteAsync(objectKey, cancellationToken);
        }
        catch (Exception exception)
        {
            // An abandoned upload's object may never have existed or may already be gone.
            _logger.LogWarning(exception, "Could not delete abandoned Drive object {ObjectKey}.", objectKey);
        }
    }
}