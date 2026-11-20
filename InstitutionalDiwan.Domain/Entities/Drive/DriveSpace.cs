using InstitutionalDiwan.Domain.Events;

namespace InstitutionalDiwan.Domain.Entities.Drive;

/// <summary>
/// A Drive storage space (Module 11): one personal space per user plus freely created shared
/// spaces. Holds the quota and the atomic usage counter; trashed items still count until they
/// are permanently deleted.
/// </summary>
public sealed class DriveSpace : AggregateRoot<Guid>
{
    public const int MaxNameLength = 150;

    public Guid SpaceId
    {
        get => Id;
        private set => Id = value;
    }

    public string Name { get; private set; } = null!;
    public int SpaceTypeId { get; private set; }
    // FK → User.UserId (InstitutionalDiwan.Domain.Entities.Identity.User)
    public Guid OwnerUserId { get; private set; }
    /// <summary>Space-specific quota; null means the institution default applies.</summary>
    public long? QuotaBytes { get; private set; }
    public long UsedBytes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? TrashedAt { get; private set; }
    public Guid? TrashedByUserId { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public bool IsTrashed => TrashedAt is not null;
    public bool IsPersonal => SpaceTypeId == DriveSpaceType.PersonalId;

    private DriveSpace()
    {
    }

    public static Result<DriveSpace> Create(
        string? name,
        int spaceTypeId,
        Guid ownerUserId,
        long? quotaBytes = null,
        DateTime? utcNow = null)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return Result.Failure<DriveSpace>(DomainErrors.Drive.SpaceNameEmpty);
        if (trimmed.Length > MaxNameLength)
            return Result.Failure<DriveSpace>(DomainErrors.Drive.SpaceNameTooLong);
        if (!DriveSpaceType.IsValid(spaceTypeId))
            return Result.Failure<DriveSpace>(DomainErrors.Drive.SpaceTypeInvalid);
        if (ownerUserId == Guid.Empty)
            return Result.Failure<DriveSpace>(DomainErrors.Drive.PrincipalIdEmpty);
        if (quotaBytes is <= 0)
            return Result.Failure<DriveSpace>(DomainErrors.Drive.QuotaInvalid);

        var now = utcNow ?? DateTime.UtcNow;
        var space = new DriveSpace
        {
            SpaceId = Guid.NewGuid(),
            Name = trimmed,
            SpaceTypeId = spaceTypeId,
            OwnerUserId = ownerUserId,
            QuotaBytes = quotaBytes,
            UsedBytes = 0,
            CreatedAt = now,
            CreatedByUserId = ownerUserId
        };
        space.RaiseDomainEvent(new DriveSpaceCreatedEvent(space.SpaceId, spaceTypeId, ownerUserId));
        return Result.Success(space);
    }

    public static Result<DriveSpace> CreatePersonal(Guid ownerUserId, long? quotaBytes = null, DateTime? utcNow = null) =>
        Create("ملفاتي", DriveSpaceType.PersonalId, ownerUserId, quotaBytes, utcNow);

    public Result Rename(string? name, DateTime? utcNow = null)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return Result.Failure(DomainErrors.Drive.SpaceNameEmpty);
        if (trimmed.Length > MaxNameLength)
            return Result.Failure(DomainErrors.Drive.SpaceNameTooLong);
        if (IsTrashed)
            return Result.Failure(DomainErrors.Drive.SpaceTrashed);

        Name = trimmed;
        UpdatedAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }

    public Result SetQuota(long? quotaBytes, DateTime? utcNow = null)
    {
        if (quotaBytes is <= 0)
            return Result.Failure(DomainErrors.Drive.QuotaInvalid);
        if (quotaBytes < UsedBytes)
            return Result.Failure(DomainErrors.Drive.QuotaBelowUsage);

        QuotaBytes = quotaBytes;
        UpdatedAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>Adds committed bytes, enforcing the effective quota (space override or institution default).</summary>
    public Result AddUsage(long bytes, long effectiveQuotaBytes, DateTime? utcNow = null)
    {
        if (bytes <= 0)
            return Result.Failure(DomainErrors.Drive.UsageInvalid);
        if (UsedBytes + bytes > effectiveQuotaBytes)
            return Result.Failure(DomainErrors.Drive.QuotaExceeded);

        UsedBytes += bytes;
        UpdatedAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>Releases bytes after a permanent deletion.</summary>
    public Result ReleaseUsage(long bytes, DateTime? utcNow = null)
    {
        if (bytes <= 0 || bytes > UsedBytes)
            return Result.Failure(DomainErrors.Drive.UsageInvalid);

        UsedBytes -= bytes;
        UpdatedAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }

    public Result Trash(Guid actorUserId, DateTime? utcNow = null)
    {
        if (IsPersonal)
            return Result.Failure(DomainErrors.Drive.CannotTrashRoot);
        if (IsTrashed)
            return Result.Failure(DomainErrors.Drive.SpaceAlreadyTrashed);

        TrashedAt = utcNow ?? DateTime.UtcNow;
        TrashedByUserId = actorUserId;
        return Result.Success();
    }

    public Result Restore(DateTime? utcNow = null)
    {
        if (!IsTrashed)
            return Result.Failure(DomainErrors.Drive.SpaceNotTrashed);

        TrashedAt = null;
        TrashedByUserId = null;
        UpdatedAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }
}