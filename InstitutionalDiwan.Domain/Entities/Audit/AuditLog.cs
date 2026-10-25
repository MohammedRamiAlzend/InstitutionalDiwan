using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace InstitutionalDiwan.Domain.Entities.Audit;

public sealed class AuditLog : Entity<long>
{
    /// <summary>
    /// The previous-hash value used by the first event in the chain. It is a 64-character string so
    /// the genesis record has exactly the same shape as every later record.
    /// </summary>
    public const string GenesisHash = "0000000000000000000000000000000000000000000000000000000000000000";

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

        var timestamp = NormalizeUtc(occurredAtUtc ?? DateTime.UtcNow);
        var module = moduleName.Trim().ToUpperInvariant();
        var normalizedEntityId = entityId.Trim();
        var prevHashNormalized = previousHash.Trim().ToLowerInvariant();
        var computedHash = ComputeSha256Hash(
            timestamp,
            actorUserId,
            actorDepartmentId,
            module,
            actionTypeId,
            entityTypeId,
            normalizedEntityId,
            relatedDocumentId,
            oldValuesJson,
            newValuesJson,
            ipAddress,
            correlationId,
            prevHashNormalized);

        var entry = new AuditLog
        {
            OccurredAtUtc = timestamp,
            ActorUserId = actorUserId,
            ActorDepartmentId = actorDepartmentId,
            ModuleName = module,
            ActionTypeId = actionTypeId,
            EntityTypeId = entityTypeId,
            EntityId = normalizedEntityId,
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

    /// <summary>
    /// Re-computes the event hash against a new predecessor. The audit interceptor calls this after
    /// it takes the database chain lock: a row appended before the lock was acquired may have chained
    /// onto a predecessor that a concurrent save has since superseded.
    /// </summary>
    public void Reseal(string previousHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(previousHash);

        PreviousHash = previousHash.Trim().ToLowerInvariant();
        EventHash = ComputeSha256Hash(
            OccurredAtUtc,
            ActorUserId,
            ActorDepartmentId,
            ModuleName,
            ActionTypeId,
            EntityTypeId,
            EntityId,
            RelatedDocumentId,
            OldValuesJson,
            NewValuesJson,
            IpAddress,
            CorrelationId,
            PreviousHash);
    }

    public Result VerifyIntegrity(string expectedPreviousHash)
    {
        if (!string.Equals(PreviousHash, expectedPreviousHash, StringComparison.OrdinalIgnoreCase))
            return Result.Failure(DomainErrors.Audit.ChainBroken);

        var recomputed = ComputeSha256Hash(
            OccurredAtUtc,
            ActorUserId,
            ActorDepartmentId,
            ModuleName,
            ActionTypeId,
            EntityTypeId,
            EntityId,
            RelatedDocumentId,
            OldValuesJson,
            NewValuesJson,
            IpAddress,
            CorrelationId,
            PreviousHash);

        return string.Equals(EventHash, recomputed, StringComparison.OrdinalIgnoreCase)
            ? Result.Success()
            : Result.Failure(DomainErrors.Audit.HashMismatch);
    }

    /// <summary>
    /// Computes the SHA-256 chain hash over every audit field except the identifier and the hash
    /// itself, so mutating any of them is detectable. The timestamp is normalized to UTC and
    /// millisecond precision so a round-trip through <c>datetime2(3)</c> cannot change the hash.
    /// </summary>
    public static string ComputeSha256Hash(
        DateTime occurredAtUtc,
        Guid? actorUserId,
        Guid? actorDepartmentId,
        string moduleName,
        int actionTypeId,
        int entityTypeId,
        string entityId,
        Guid? relatedDocumentId,
        string? oldValuesJson,
        string? newValuesJson,
        string? ipAddress,
        string? correlationId,
        string previousHash)
    {
        var timestamp = NormalizeUtc(occurredAtUtc);
        var action = actionTypeId.ToString(CultureInfo.InvariantCulture);
        var entity = entityTypeId.ToString(CultureInfo.InvariantCulture);
        var payload = string.Join('|',
            Encode(timestamp.ToString("O", CultureInfo.InvariantCulture)),
            Encode(actorUserId?.ToString("D", CultureInfo.InvariantCulture)),
            Encode(actorDepartmentId?.ToString("D", CultureInfo.InvariantCulture)),
            Encode(moduleName),
            Encode(action),
            Encode(entity),
            Encode(entityId),
            Encode(relatedDocumentId?.ToString("D", CultureInfo.InvariantCulture)),
            Encode(oldValuesJson),
            Encode(newValuesJson),
            Encode(ipAddress),
            Encode(correlationId),
            Encode(previousHash));

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Length-prefixes each field so a value containing the separator cannot shift the field
    /// boundaries and produce an ambiguous payload. Null is distinct from the empty string.
    /// </summary>
    private static string Encode(string? value) =>
        value is null ? "-" : $"{value.Length}:{value}";

    /// <summary>
    /// Normalizes a timestamp to UTC with millisecond precision. EF rounds <c>datetime2(3)</c>
    /// values, so hashing anything finer would fail verification after a database round-trip.
    /// </summary>
    private static DateTime NormalizeUtc(DateTime value)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

        return new DateTime(utc.Ticks - (utc.Ticks % TimeSpan.TicksPerMillisecond), DateTimeKind.Utc);
    }
}
