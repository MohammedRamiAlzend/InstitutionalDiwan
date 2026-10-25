namespace InstitutionalDiwan.Application.Features.Attachments;

/// <summary>Result of re-computing a stored version's SHA-256 and comparing it with the recorded one.</summary>
public sealed record AttachmentChecksumResponse(
    Guid FileId,
    Guid VersionId,
    int VersionNumber,
    string RecordedChecksumSha256,
    string ComputedChecksumSha256)
{
    /// <summary>True when the bytes on disk still produce the checksum recorded at upload time.</summary>
    public bool IsMatch => string.Equals(
        RecordedChecksumSha256,
        ComputedChecksumSha256,
        StringComparison.OrdinalIgnoreCase);
}
