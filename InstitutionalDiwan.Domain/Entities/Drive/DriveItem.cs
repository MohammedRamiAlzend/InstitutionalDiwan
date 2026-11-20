using InstitutionalDiwan.Domain.Events;

namespace InstitutionalDiwan.Domain.Entities.Drive;

/// <summary>
/// A unified Drive tree node (Module 11): folder or file in one table, so move, trash and
/// permission inheritance work the same way for both. A file points at its current committed
/// version; trashing a folder implicitly trashes its subtree (queries exclude trashed ancestors).
/// </summary>
public sealed class DriveItem : AggregateRoot<Guid>
{
    public const int MaxNameLength = 255;
    public const int MaxDepth = 30;
    private static readonly char[] ForbiddenNameChars = { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };

    public Guid ItemId
    {
        get => Id;
        private set => Id = value;
    }

    // FK → DriveSpace.SpaceId
    public Guid SpaceId { get; private set; }
    // FK → DriveItem.ItemId (parent folder); null = space root level
    public Guid? ParentId { get; private set; }
    public int ItemKindId { get; private set; }
    public string Name { get; private set; } = null!;
    // FK → User.UserId
    public Guid OwnerUserId { get; private set; }
    // FK → DriveFileVersion.VersionId; null until the first version commits (files only)
    public Guid? CurrentVersionId { get; private set; }
    /// <summary>Size of the current version; files only, zero until first commit.</summary>
    public long SizeBytes { get; private set; }
    public bool BreakInheritance { get; private set; }
    public DateTime? TrashedAt { get; private set; }
    public Guid? TrashedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public bool IsTrashed => TrashedAt is not null;
    public bool IsFile => ItemKindId == DriveItemKind.FileId;
    public bool IsFolder => ItemKindId == DriveItemKind.FolderId;

    private DriveItem()
    {
    }

    public static Result<DriveItem> CreateFolder(
        Guid spaceId,
        Guid? parentId,
        string? name,
        Guid ownerUserId,
        int depth = 1,
        DateTime? utcNow = null) =>
        Create(spaceId, parentId, name, ownerUserId, DriveItemKind.FolderId, depth, utcNow);

    public static Result<DriveItem> CreateFile(
        Guid spaceId,
        Guid? parentId,
        string? name,
        Guid ownerUserId,
        int depth = 1,
        DateTime? utcNow = null) =>
        Create(spaceId, parentId, name, ownerUserId, DriveItemKind.FileId, depth, utcNow);

    private static Result<DriveItem> Create(
        Guid spaceId,
        Guid? parentId,
        string? name,
        Guid ownerUserId,
        int itemKindId,
        int depth,
        DateTime? utcNow)
    {
        if (spaceId == Guid.Empty)
            return Result.Failure<DriveItem>(DomainErrors.Drive.SpaceNotFound);
        if (ownerUserId == Guid.Empty)
            return Result.Failure<DriveItem>(DomainErrors.Drive.PrincipalIdEmpty);
        if (depth > MaxDepth)
            return Result.Failure<DriveItem>(DomainErrors.Drive.DepthLimitReached);

        var nameCheck = ValidateName(name);
        if (nameCheck.IsFailure)
            return Result.Failure<DriveItem>(nameCheck.Error);

        var now = utcNow ?? DateTime.UtcNow;
        var item = new DriveItem
        {
            ItemId = Guid.NewGuid(),
            SpaceId = spaceId,
            ParentId = parentId,
            ItemKindId = itemKindId,
            Name = name!.Trim(),
            OwnerUserId = ownerUserId,
            CreatedAt = now,
            CreatedByUserId = ownerUserId
        };
        return Result.Success(item);
    }

    public Result Rename(string? name, DateTime? utcNow = null)
    {
        if (IsTrashed)
            return Result.Failure(DomainErrors.Drive.ItemTrashed);

        var nameCheck = ValidateName(name);
        if (nameCheck.IsFailure)
            return nameCheck;

        Name = name!.Trim();
        UpdatedAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Moves the node under a new parent (possibly in another space). The caller supplies the
    /// target's ancestor chain and depth so cycles and the depth limit are checked in the domain.
    /// </summary>
    public Result Move(
        Guid newSpaceId,
        Guid? newParentId,
        IReadOnlyCollection<Guid> targetAncestorIds,
        int targetDepth,
        DateTime? utcNow = null)
    {
        if (IsTrashed)
            return Result.Failure(DomainErrors.Drive.ItemTrashed);
        if (newSpaceId == Guid.Empty)
            return Result.Failure(DomainErrors.Drive.SpaceNotFound);
        if (newParentId == ItemId || targetAncestorIds.Contains(ItemId))
            return Result.Failure(DomainErrors.Drive.CycleDetected);
        if (targetDepth > MaxDepth)
            return Result.Failure(DomainErrors.Drive.DepthLimitReached);

        SpaceId = newSpaceId;
        ParentId = newParentId;
        UpdatedAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>Points a file at a newly committed version and records its size.</summary>
    public Result SetCurrentVersion(
        Guid versionId,
        int versionNumber,
        long sizeBytes,
        string? checksumSha256,
        DateTime? utcNow = null)
    {
        if (!IsFile)
            return Result.Failure(DomainErrors.Drive.NotAFile);
        if (IsTrashed)
            return Result.Failure(DomainErrors.Drive.ItemTrashed);
        if (versionId == Guid.Empty || versionNumber <= 0)
            return Result.Failure(DomainErrors.Drive.VersionNumberInvalid);
        if (sizeBytes <= 0)
            return Result.Failure(DomainErrors.Drive.VersionSizeInvalid);
        if (string.IsNullOrWhiteSpace(checksumSha256) || checksumSha256.Trim().Length != 64)
            return Result.Failure(DomainErrors.Drive.ChecksumInvalid);

        CurrentVersionId = versionId;
        SizeBytes = sizeBytes;
        UpdatedAt = utcNow ?? DateTime.UtcNow;
        RaiseDomainEvent(new DriveFileVersionCommittedEvent(ItemId, versionId, versionNumber, checksumSha256.Trim().ToLowerInvariant()));
        return Result.Success();
    }

    public Result BreakPermissionInheritance(DateTime? utcNow = null)
    {
        if (IsTrashed)
            return Result.Failure(DomainErrors.Drive.ItemTrashed);

        BreakInheritance = true;
        UpdatedAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }

    public Result RestorePermissionInheritance(DateTime? utcNow = null)
    {
        if (IsTrashed)
            return Result.Failure(DomainErrors.Drive.ItemTrashed);
        if (!BreakInheritance)
            return Result.Failure(DomainErrors.Drive.InheritanceBroken);

        BreakInheritance = false;
        UpdatedAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }

    public Result Trash(Guid actorUserId, DateTime? utcNow = null)
    {
        if (IsTrashed)
            return Result.Failure(DomainErrors.Drive.ItemAlreadyTrashed);

        TrashedAt = utcNow ?? DateTime.UtcNow;
        TrashedByUserId = actorUserId;
        RaiseDomainEvent(new DriveItemTrashedEvent(ItemId, SpaceId, actorUserId));
        return Result.Success();
    }

    public Result Restore(DateTime? utcNow = null)
    {
        if (!IsTrashed)
            return Result.Failure(DomainErrors.Drive.ItemNotTrashed);

        TrashedAt = null;
        TrashedByUserId = null;
        UpdatedAt = utcNow ?? DateTime.UtcNow;
        RaiseDomainEvent(new DriveItemRestoredEvent(ItemId, SpaceId));
        return Result.Success();
    }

    private static Result ValidateName(string? name)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return Result.Failure(DomainErrors.Drive.ItemNameEmpty);
        if (trimmed.Length > MaxNameLength)
            return Result.Failure(DomainErrors.Drive.ItemNameTooLong);
        if (trimmed is "." or ".." || trimmed.IndexOfAny(ForbiddenNameChars) >= 0)
            return Result.Failure(DomainErrors.Drive.ItemNameInvalid);
        return Result.Success();
    }
}