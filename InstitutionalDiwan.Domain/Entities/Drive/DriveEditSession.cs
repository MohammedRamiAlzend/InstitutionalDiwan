namespace InstitutionalDiwan.Domain.Entities.Drive;

/// <summary>
/// An OnlyOffice editing session on a Drive file (Module 11). The document key is unique per
/// (item, version) as OnlyOffice requires; the base version lets the save callback reject
/// conflicting saves after the file moved on.
/// </summary>
public sealed class DriveEditSession : Entity<Guid>
{
    public const int MaxDocumentKeyLength = 200;

    public Guid SessionId
    {
        get => Id;
        private set => Id = value;
    }

    // FK → DriveItem.ItemId (a file item)
    public Guid FileItemId { get; private set; }
    // FK → User.UserId
    public Guid UserId { get; private set; }
    public string DocumentKey { get; private set; } = null!;
    // FK → DriveFileVersion.VersionId the session was opened against
    public Guid BaseVersionId { get; private set; }
    public int StatusId { get; private set; }
    public DateTime OpenedAt { get; private set; }
    public DateTime LastActivityAt { get; private set; }

    public bool IsOpen => StatusId == DriveEditSessionStatus.OpenId;

    private DriveEditSession()
    {
    }

    public static Result<DriveEditSession> Open(
        Guid fileItemId,
        Guid userId,
        string? documentKey,
        Guid baseVersionId,
        DateTime? utcNow = null)
    {
        if (fileItemId == Guid.Empty)
            return Result.Failure<DriveEditSession>(DomainErrors.Drive.ItemNotFound);
        if (userId == Guid.Empty)
            return Result.Failure<DriveEditSession>(DomainErrors.Drive.PrincipalIdEmpty);
        if (baseVersionId == Guid.Empty)
            return Result.Failure<DriveEditSession>(DomainErrors.Drive.VersionNotFound);

        var trimmedKey = documentKey?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedKey))
            return Result.Failure<DriveEditSession>(DomainErrors.Drive.DocumentKeyEmpty);
        if (trimmedKey.Length > MaxDocumentKeyLength)
            return Result.Failure<DriveEditSession>(DomainErrors.Drive.DocumentKeyTooLong);

        var now = utcNow ?? DateTime.UtcNow;
        var session = new DriveEditSession
        {
            SessionId = Guid.NewGuid(),
            FileItemId = fileItemId,
            UserId = userId,
            DocumentKey = trimmedKey,
            BaseVersionId = baseVersionId,
            StatusId = DriveEditSessionStatus.OpenId,
            OpenedAt = now,
            LastActivityAt = now
        };
        return Result.Success(session);
    }

    public Result Touch(DateTime? utcNow = null)
    {
        if (!IsOpen)
            return Result.Failure(DomainErrors.Drive.SessionNotOpen);

        LastActivityAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }

    public Result MarkSaved(DateTime? utcNow = null)
    {
        if (!IsOpen)
            return Result.Failure(DomainErrors.Drive.SessionNotOpen);

        StatusId = DriveEditSessionStatus.SavedId;
        LastActivityAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }

    public Result Close(DateTime? utcNow = null)
    {
        if (StatusId is not (DriveEditSessionStatus.OpenId or DriveEditSessionStatus.SavedId))
            return Result.Failure(DomainErrors.Drive.SessionNotOpen);

        StatusId = DriveEditSessionStatus.ClosedId;
        LastActivityAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }

    public Result Expire(DateTime? utcNow = null)
    {
        if (!IsOpen)
            return Result.Failure(DomainErrors.Drive.SessionNotOpen);

        StatusId = DriveEditSessionStatus.ExpiredId;
        LastActivityAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }
}