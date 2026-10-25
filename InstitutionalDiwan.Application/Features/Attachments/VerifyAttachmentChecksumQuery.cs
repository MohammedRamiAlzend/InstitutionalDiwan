using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Attachments;

/// <summary>
/// Re-reads a stored version and recomputes its SHA-256, proving the bytes on disk still match the
/// checksum recorded at upload time. Omit the version to verify the current one.
/// </summary>
public sealed record VerifyAttachmentChecksumQuery(
    Guid FileId,
    Guid UserId,
    Guid? VersionId = null) : IQuery<AttachmentChecksumResponse>;
