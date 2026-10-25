using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Audit;
using InstitutionalDiwan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Repositories;

/// <summary>
/// Append-only access to the audit chain and the anchors/reports derived from it. Reads use
/// <c>AsNoTracking</c> so no query can accidentally mutate a historical entry; only the three
/// explicit <c>Add</c> methods add rows.
/// </summary>
internal sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly DiwanDbContext _context;

    public AuditLogRepository(DiwanDbContext context) => _context = context;

    public Task<AuditLog?> GetLatestAsync(CancellationToken cancellationToken = default) =>
        _context.AuditLogs
            .AsNoTracking()
            .OrderByDescending(log => log.AuditLogId)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<AuditLog?> GetByIdAsync(
        long auditLogId,
        CancellationToken cancellationToken = default) =>
        _context.AuditLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(log => log.AuditLogId == auditLogId, cancellationToken);

    public async Task<PagedList<AuditLog>> SearchAsync(
        AuditLogFilter filter,
        PageRequest page,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.ModuleName))
        {
            var module = filter.ModuleName.Trim().ToUpperInvariant();
            query = query.Where(log => log.ModuleName == module);
        }

        if (filter.ActionTypeId is { } actionTypeId)
            query = query.Where(log => log.ActionTypeId == actionTypeId);

        if (filter.EntityTypeId is { } entityTypeId)
            query = query.Where(log => log.EntityTypeId == entityTypeId);

        if (!string.IsNullOrWhiteSpace(filter.EntityId))
        {
            var entityId = filter.EntityId.Trim();
            query = query.Where(log => log.EntityId == entityId);
        }

        if (filter.ActorUserId is { } actorUserId)
            query = query.Where(log => log.ActorUserId == actorUserId);

        if (filter.ActorDepartmentId is { } actorDepartmentId)
            query = query.Where(log => log.ActorDepartmentId == actorDepartmentId);

        if (filter.RelatedDocumentId is { } relatedDocumentId)
            query = query.Where(log => log.RelatedDocumentId == relatedDocumentId);

        if (!string.IsNullOrWhiteSpace(filter.CorrelationId))
        {
            var correlationId = filter.CorrelationId.Trim();
            query = query.Where(log => log.CorrelationId == correlationId);
        }

        if (!string.IsNullOrWhiteSpace(filter.IpAddress))
        {
            var ipAddress = filter.IpAddress.Trim();
            query = query.Where(log => log.IpAddress == ipAddress);
        }

        if (filter.FromUtc is { } fromUtc)
            query = query.Where(log => log.OccurredAtUtc >= fromUtc);

        if (filter.ToUtc is { } toUtc)
            query = query.Where(log => log.OccurredAtUtc <= toUtc);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(log =>
                log.ModuleName.Contains(search) ||
                log.EntityId.Contains(search) ||
                (log.CorrelationId != null && log.CorrelationId.Contains(search)) ||
                (log.IpAddress != null && log.IpAddress.Contains(search)));
        }

        query = ApplySort(query, page);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip(page.Skip).Take(page.Take).ToListAsync(cancellationToken);

        return new PagedList<AuditLog>(items, totalCount);
    }

    public async Task<IReadOnlyList<AuditLog>> ListRangeAsync(
        long fromAuditLogId,
        long toAuditLogId,
        CancellationToken cancellationToken = default) =>
        await _context.AuditLogs
            .AsNoTracking()
            .Where(log => log.AuditLogId >= fromAuditLogId && log.AuditLogId <= toAuditLogId)
            .OrderBy(log => log.AuditLogId)
            .ToListAsync(cancellationToken);

    public Task<AuditCheckpoint?> GetLatestCheckpointAsync(
        CancellationToken cancellationToken = default) =>
        _context.AuditCheckpoints
            .AsNoTracking()
            .OrderByDescending(checkpoint => checkpoint.ToAuditLogId)
            .ThenByDescending(checkpoint => checkpoint.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedList<AuditCheckpoint>> ListCheckpointsAsync(
        PageRequest page,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AuditCheckpoints
            .AsNoTracking()
            .OrderByDescending(checkpoint => checkpoint.ToAuditLogId)
            .ThenByDescending(checkpoint => checkpoint.CreatedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip(page.Skip).Take(page.Take).ToListAsync(cancellationToken);

        return new PagedList<AuditCheckpoint>(items, totalCount);
    }

    public async Task<PagedList<AuditIntegrityCheck>> ListIntegrityChecksAsync(
        PageRequest page,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AuditIntegrityChecks
            .AsNoTracking()
            .OrderByDescending(check => check.ExecutedAtUtc)
            .ThenByDescending(check => check.CheckId);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip(page.Skip).Take(page.Take).ToListAsync(cancellationToken);

        return new PagedList<AuditIntegrityCheck>(items, totalCount);
    }

    public void Add(AuditLog auditLog) => _context.AuditLogs.Add(auditLog);

    public void AddCheckpoint(AuditCheckpoint checkpoint) =>
        _context.AuditCheckpoints.Add(checkpoint);

    public void AddIntegrityCheck(AuditIntegrityCheck integrityCheck) =>
        _context.AuditIntegrityChecks.Add(integrityCheck);

    private static IQueryable<AuditLog> ApplySort(IQueryable<AuditLog> query, PageRequest page)
    {
        var descending = page.NormalizedSortBy is null || page.SortDescending;

        return page.NormalizedSortBy switch
        {
            "occurredat" or "occurred_at" => descending
                ? query.OrderByDescending(log => log.OccurredAtUtc).ThenByDescending(log => log.AuditLogId)
                : query.OrderBy(log => log.OccurredAtUtc).ThenBy(log => log.AuditLogId),
            "module" or "modulename" => descending
                ? query.OrderByDescending(log => log.ModuleName).ThenByDescending(log => log.AuditLogId)
                : query.OrderBy(log => log.ModuleName).ThenBy(log => log.AuditLogId),
            "action" or "actiontype" => descending
                ? query.OrderByDescending(log => log.ActionTypeId).ThenByDescending(log => log.AuditLogId)
                : query.OrderBy(log => log.ActionTypeId).ThenBy(log => log.AuditLogId),
            "entity" or "entitytype" => descending
                ? query.OrderByDescending(log => log.EntityTypeId).ThenByDescending(log => log.AuditLogId)
                : query.OrderBy(log => log.EntityTypeId).ThenBy(log => log.AuditLogId),
            _ => descending
                ? query.OrderByDescending(log => log.AuditLogId)
                : query.OrderBy(log => log.AuditLogId)
        };
    }
}
