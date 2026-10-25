using InstitutionalDiwan.Application.Abstractions.Context;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Audit;
using InstitutionalDiwan.Domain.Repositories;
using System.Reflection;

namespace InstitutionalDiwan.Tests.Support;

/// <summary>In-memory audit chain used by handler tests; assigns the identity ids the database would.</summary>
internal sealed class FakeAuditLogRepository : IAuditLogRepository
{
    private long _nextAuditLogId = 1;

    public List<AuditLog> Logs { get; } = new();
    public List<AuditCheckpoint> Checkpoints { get; } = new();
    public List<AuditIntegrityCheck> IntegrityChecks { get; } = new();

    public void Add(AuditLog auditLog)
    {
        typeof(AuditLog)
            .GetProperty(nameof(AuditLog.AuditLogId))!
            .SetValue(auditLog, _nextAuditLogId++);

        Logs.Add(auditLog);
    }

    public void AddCheckpoint(AuditCheckpoint checkpoint) => Checkpoints.Add(checkpoint);

    public void AddIntegrityCheck(AuditIntegrityCheck integrityCheck) =>
        IntegrityChecks.Add(integrityCheck);

    public Task<AuditLog?> GetLatestAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Logs.OrderByDescending(log => log.AuditLogId).FirstOrDefault());

    public Task<AuditLog?> GetByIdAsync(
        long auditLogId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Logs.FirstOrDefault(log => log.AuditLogId == auditLogId));

    public Task<PagedList<AuditLog>> SearchAsync(
        AuditLogFilter filter,
        PageRequest page,
        CancellationToken cancellationToken = default)
    {
        var query = Logs.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filter.ModuleName))
            query = query.Where(log => log.ModuleName == filter.ModuleName.Trim().ToUpperInvariant());
        if (filter.ActionTypeId is { } action)
            query = query.Where(log => log.ActionTypeId == action);
        if (filter.EntityTypeId is { } entityType)
            query = query.Where(log => log.EntityTypeId == entityType);
        if (!string.IsNullOrWhiteSpace(filter.EntityId))
            query = query.Where(log => log.EntityId == filter.EntityId.Trim());
        if (filter.ActorUserId is { } actor)
            query = query.Where(log => log.ActorUserId == actor);
        if (filter.RelatedDocumentId is { } documentId)
            query = query.Where(log => log.RelatedDocumentId == documentId);
        if (filter.FromUtc is { } from)
            query = query.Where(log => log.OccurredAtUtc >= from);
        if (filter.ToUtc is { } to)
            query = query.Where(log => log.OccurredAtUtc <= to);
        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(log =>
                log.ModuleName.Contains(filter.Search, StringComparison.OrdinalIgnoreCase) ||
                log.EntityId.Contains(filter.Search, StringComparison.OrdinalIgnoreCase));

        var ordered = page.SortDescending
            ? query.OrderByDescending(log => log.AuditLogId)
            : query.OrderBy(log => log.AuditLogId);

        var total = ordered.Count();
        var items = ordered.Skip(page.Skip).Take(page.Take).ToList();

        return Task.FromResult(new PagedList<AuditLog>(items, total));
    }

    public Task<IReadOnlyList<AuditLog>> ListRangeAsync(
        long fromAuditLogId,
        long toAuditLogId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AuditLog>>(Logs
            .Where(log => log.AuditLogId >= fromAuditLogId && log.AuditLogId <= toAuditLogId)
            .OrderBy(log => log.AuditLogId)
            .ToList());

    public Task<AuditCheckpoint?> GetLatestCheckpointAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Checkpoints.OrderByDescending(checkpoint => checkpoint.ToAuditLogId).FirstOrDefault());

    public Task<PagedList<AuditCheckpoint>> ListCheckpointsAsync(
        PageRequest page,
        CancellationToken cancellationToken = default)
    {
        var items = Checkpoints.OrderByDescending(checkpoint => checkpoint.ToAuditLogId).ToList();
        return Task.FromResult(new PagedList<AuditCheckpoint>(
            items.Skip(page.Skip).Take(page.Take).ToList(), items.Count));
    }

    public Task<PagedList<AuditIntegrityCheck>> ListIntegrityChecksAsync(
        PageRequest page,
        CancellationToken cancellationToken = default)
    {
        var items = IntegrityChecks.OrderByDescending(check => check.ExecutedAtUtc).ToList();
        return Task.FromResult(new PagedList<AuditIntegrityCheck>(
            items.Skip(page.Skip).Take(page.Take).ToList(), items.Count));
    }

    /// <summary>Mutates a stored row through reflection, simulating an attacker with database access.</summary>
    public void TamperModuleName(long auditLogId, string moduleName)
    {
        var log = Logs.Single(item => item.AuditLogId == auditLogId);
        typeof(AuditLog)
            .GetProperty(nameof(AuditLog.ModuleName))!
            .SetValue(log, moduleName);
    }
}

internal sealed class FakeRequestContext : IRequestContext
{
    public string? CorrelationId { get; set; } = "test-correlation";
    public string? IpAddress { get; set; } = "10.0.0.15";
}
