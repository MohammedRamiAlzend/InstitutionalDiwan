using System;
using System.Security.Cryptography;
using System.Text;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;

namespace InstitutionalDiwan.Domain.Entities.Audit;

public sealed class AuditLog : Entity<long>
{
    public long AuditLogId
    {
        get => Id;
        private set => Id = value;
    }

    public DateTime OccurredAtUtc { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public Guid? ActorDepartmentId { get; private set; }
    public string ModuleName { get; private set; } = null!;
    public string ActionType { get; private set; } = null!;
    public string EntityType { get; private set; } = null!;
    public string EntityId { get; private set; } = null!;
    public Guid? RelatedDocumentId { get; private set; }
    public string? OldValuesJson { get; private set; }
    public string? NewValuesJson { get; private set; }
    public string? IpAddress { get; private set; }
    public string? CorrelationId { get; private set; }
    public string PreviousHash { get; private set; } = null!;
    public string EventHash { get; private set; } = null!;

    private AuditLog()
    {
    }

    public static Result<AuditLog> Record(
        string? moduleName,
        string? actionType,
        string? entityType,
        string? entityId,
        string? previousHash,
        Guid? actorUserId = null,
        Guid? actorDepartmentId = null,
        Guid? relatedDocumentId = null,
        string? oldValuesJson = null,
        string? newValuesJson = null,
        string? ipAddress = null,
        string? correlationId = null,
        DateTime? occurredAtUtc = null)
    {
        if (string.IsNullOrWhiteSpace(moduleName) ||
            string.IsNullOrWhiteSpace(actionType) ||
            string.IsNullOrWhiteSpace(entityType) ||
            string.IsNullOrWhiteSpace(entityId) ||
            string.IsNullOrWhiteSpace(previousHash))
        {
            return Result.Failure<AuditLog>(DomainErrors.Audit.RequiredFieldsMissing);
        }

        var timestamp = occurredAtUtc ?? DateTime.UtcNow;
        var prevHashNormalized = previousHash.Trim().ToLowerInvariant();
        var computedHash = ComputeSha256Hash(
            timestamp,
            actorUserId,
            moduleName.Trim().ToUpperInvariant(),
            actionType.Trim().ToUpperInvariant(),
            entityType.Trim().ToUpperInvariant(),
            entityId.Trim(),
            oldValuesJson,
            newValuesJson,
            prevHashNormalized);

        var entry = new AuditLog
        {
            OccurredAtUtc = timestamp,
            ActorUserId = actorUserId,
            ActorDepartmentId = actorDepartmentId,
            ModuleName = moduleName.Trim().ToUpperInvariant(),
            ActionType = actionType.Trim().ToUpperInvariant(),
            EntityType = entityType.Trim().ToUpperInvariant(),
            EntityId = entityId.Trim(),
            RelatedDocumentId = relatedDocumentId,
            OldValuesJson = oldValuesJson,
            NewValuesJson = newValuesJson,
            IpAddress = ipAddress,
            CorrelationId = correlationId,
            PreviousHash = prevHashNormalized,
            EventHash = computedHash
        };

        return Result.Success(entry);
    }

    public Result VerifyIntegrity(string expectedPreviousHash)
    {
        if (!string.Equals(PreviousHash, expectedPreviousHash, StringComparison.OrdinalIgnoreCase))
            return Result.Failure(DomainErrors.Audit.ChainBroken);

        var recomputed = ComputeSha256Hash(
            OccurredAtUtc,
            ActorUserId,
            ModuleName,
            ActionType,
            EntityType,
            EntityId,
            OldValuesJson,
            NewValuesJson,
            PreviousHash);

        return string.Equals(EventHash, recomputed, StringComparison.OrdinalIgnoreCase)
            ? Result.Success()
            : Result.Failure(DomainErrors.Audit.HashMismatch);
    }

    public static string ComputeSha256Hash(
        DateTime occurredAtUtc,
        Guid? actorUserId,
        string moduleName,
        string actionType,
        string entityType,
        string entityId,
        string? oldValuesJson,
        string? newValuesJson,
        string previousHash)
    {
        var payload = $"{occurredAtUtc:O}|{actorUserId}|{moduleName}|{actionType}|{entityType}|{entityId}|{oldValuesJson}|{newValuesJson}|{previousHash}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
