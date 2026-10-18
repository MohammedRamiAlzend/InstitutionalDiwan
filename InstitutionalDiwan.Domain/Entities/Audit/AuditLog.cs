using System;
using System.Globalization;
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
    // FK → User.UserId (InstitutionalDiwan.Domain.Entities.Identity.User)
    public Guid? ActorUserId { get; private set; }
    // FK → Department.DepartmentId (InstitutionalDiwan.Domain.Entities.Organization.Department)
    public Guid? ActorDepartmentId { get; private set; }
    public string ModuleName { get; private set; } = null!;
    // FK → AuditActionType.Id (InstitutionalDiwan.Domain.Entities.Audit.AuditActionType)
    public int ActionTypeId { get; private set; }
    // FK → AuditEntityType.Id (InstitutionalDiwan.Domain.Entities.Audit.AuditEntityType)
    public int EntityTypeId { get; private set; }
    // Polymorphic reference: id of the entity identified by EntityTypeId (no single FK)
    public string EntityId { get; private set; } = null!;
    // FK → Document.DocumentId (InstitutionalDiwan.Domain.Entities.Correspondence.Document)
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
        int actionTypeId,
        int entityTypeId,
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
            actionTypeId <= 0 ||
            entityTypeId <= 0 ||
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
            actionTypeId,
            entityTypeId,
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
            ActionTypeId = actionTypeId,
            EntityTypeId = entityTypeId,
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
            ActionTypeId,
            EntityTypeId,
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
        int actionTypeId,
        int entityTypeId,
        string entityId,
        string? oldValuesJson,
        string? newValuesJson,
        string previousHash)
    {
        var action = actionTypeId.ToString(CultureInfo.InvariantCulture);
        var entity = entityTypeId.ToString(CultureInfo.InvariantCulture);
        var payload = $"{occurredAtUtc:O}|{actorUserId}|{moduleName}|{action}|{entity}|{entityId}|{oldValuesJson}|{newValuesJson}|{previousHash}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
