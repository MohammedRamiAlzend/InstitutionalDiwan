using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.ValueObjects;

namespace InstitutionalDiwan.Domain.Entities.Diagnostics;

/// <summary>Where an error happened. Stored as its integer value.</summary>
public enum SystemErrorSource
{
    Server = 1,
    Client = 2
}

/// <summary>What kind of failure was captured. Stored as its integer value.</summary>
public enum SystemErrorKind
{
    /// <summary>An unexpected server exception, or a 5xx answer.</summary>
    ServerException = 1,
    /// <summary>A request the server refused: 400, 403, 404, 409 and similar.</summary>
    ServerRejection = 2,
    /// <summary>A page failed to render in the browser.</summary>
    ClientRender = 3,
    /// <summary>An uncaught script error or promise rejection in the browser.</summary>
    ClientScript = 4,
    /// <summary>A request the browser could not complete, or that answered 5xx.</summary>
    ClientNetwork = 5
}

/// <summary>Where a developer stands with an error. Stored as its integer value.</summary>
public enum SystemErrorStatus
{
    New = 1,
    InProgress = 2,
    Resolved = 3
}

/// <summary>
/// One distinct error, grouped by fingerprint: repeats raise <see cref="OccurrenceCount"/> and refresh
/// the latest sample instead of adding rows. An error seen again after being resolved reopens as new.
/// </summary>
public sealed class SystemError : Entity<Guid>
{
    public const int FingerprintLength = 64;
    public const int MaxTitleLength = 300;
    public const int MaxMessageLength = 2000;
    public const int MaxDetailsLength = 16000;
    public const int MaxPathLength = 500;
    public const int MaxNotesLength = 4000;

    public Guid SystemErrorId
    {
        get => Id;
        private set => Id = value;
    }

    public string Fingerprint { get; private set; } = null!;
    public SystemErrorSource Source { get; private set; }
    public SystemErrorKind Kind { get; private set; }
    /// <summary>Exception type or domain error code.</summary>
    public string Title { get; private set; } = null!;
    public string Message { get; private set; } = null!;
    /// <summary>Stack trace of the latest occurrence.</summary>
    public string? Details { get; private set; }
    /// <summary>The API path or browser page of the latest occurrence, without its query string.</summary>
    public string? Path { get; private set; }
    public string? HttpMethod { get; private set; }
    public int? StatusCode { get; private set; }
    public Guid? LastUserId { get; private set; }
    public string? LastUsername { get; private set; }
    public string? UserAgent { get; private set; }
    public string? TraceId { get; private set; }
    public int OccurrenceCount { get; private set; }
    public DateTime FirstSeenAt { get; private set; }
    public DateTime LastSeenAt { get; private set; }
    public SystemErrorStatus Status { get; private set; } = SystemErrorStatus.New;
    public string? Notes { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private SystemError()
    {
    }

    public static SystemError Create(SystemErrorOccurrence occurrence) =>
        new()
        {
            SystemErrorId = Guid.NewGuid(),
            Fingerprint = occurrence.Fingerprint,
            Source = occurrence.Source,
            Kind = occurrence.Kind,
            Title = occurrence.Title,
            Message = occurrence.Message,
            Details = occurrence.Details,
            Path = occurrence.Path,
            HttpMethod = occurrence.HttpMethod,
            StatusCode = occurrence.StatusCode,
            LastUserId = occurrence.UserId,
            LastUsername = occurrence.Username,
            UserAgent = occurrence.UserAgent,
            TraceId = occurrence.TraceId,
            OccurrenceCount = 1,
            FirstSeenAt = occurrence.OccurredAt,
            LastSeenAt = occurrence.OccurredAt
        };

    /// <summary>Records how far a developer got with this error.</summary>
    public Result UpdateWork(SystemErrorStatus status, string? notes, Guid actorUserId, DateTime utcNow)
    {
        if (!Enum.IsDefined(status))
            return Result.Failure(DomainErrors.SystemErrors.StatusInvalid);

        var cleaned = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        if (cleaned?.Length > MaxNotesLength)
            return Result.Failure(DomainErrors.SystemErrors.NotesTooLong);

        ResolvedAt = status == SystemErrorStatus.Resolved
            ? (Status == SystemErrorStatus.Resolved ? ResolvedAt : utcNow)
            : null;
        Status = status;
        Notes = cleaned;
        UpdatedByUserId = actorUserId;
        UpdatedAt = utcNow;
        return Result.Success();
    }
}
