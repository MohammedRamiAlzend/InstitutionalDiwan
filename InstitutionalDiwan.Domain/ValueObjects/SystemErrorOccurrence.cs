using InstitutionalDiwan.Domain.Entities.Diagnostics;

namespace InstitutionalDiwan.Domain.ValueObjects;

/// <summary>One observed failure, ready to be merged into the <see cref="SystemError"/> sharing its fingerprint.</summary>
public sealed record SystemErrorOccurrence(
    string Fingerprint,
    SystemErrorSource Source,
    SystemErrorKind Kind,
    string Title,
    string Message,
    string? Details,
    string? Path,
    string? HttpMethod,
    int? StatusCode,
    Guid? UserId,
    string? Username,
    string? UserAgent,
    string? TraceId,
    DateTime OccurredAt);
