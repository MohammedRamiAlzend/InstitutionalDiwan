using InstitutionalDiwan.Domain.Entities.Drive;
using InstitutionalDiwan.Domain.Entities.Organization;

namespace InstitutionalDiwan.Application.Features.Drive;

/// <summary>The caller's Drive overview: their personal space (if provisioned) plus accessible shared spaces.</summary>
public sealed record DriveSpacesOverviewResponse(
    DriveSpaceResponse? Personal,
    IReadOnlyList<DriveSpaceResponse> Shared);

/// <summary>A Drive space with its quota and atomic usage counter.</summary>
public sealed record DriveSpaceResponse(
    Guid SpaceId,
    string Name,
    int SpaceTypeId,
    Guid OwnerUserId,
    long? QuotaBytes,
    long EffectiveQuotaBytes,
    long UsedBytes,
    DateTime CreatedAt,
    Guid CreatedByUserId,
    DateTime? UpdatedAt,
    DateTime? TrashedAt)
{
    public static DriveSpaceResponse From(DriveSpace space, long effectiveQuotaBytes) => new(
        space.SpaceId, space.Name, space.SpaceTypeId, space.OwnerUserId,
        space.QuotaBytes, effectiveQuotaBytes, space.UsedBytes,
        space.CreatedAt, space.CreatedByUserId, space.UpdatedAt, space.TrashedAt);
}

/// <summary>A Drive tree node (folder or file). <paramref name="Starred"/> is per-caller.</summary>
public sealed record DriveItemResponse(
    Guid ItemId,
    Guid SpaceId,
    Guid? ParentId,
    int ItemKindId,
    string Name,
    Guid OwnerUserId,
    Guid? CurrentVersionId,
    long SizeBytes,
    bool BreakInheritance,
    bool Starred,
    DateTime? TrashedAt,
    DateTime CreatedAt,
    Guid CreatedByUserId,
    DateTime? UpdatedAt)
{
    public static DriveItemResponse From(DriveItem item, bool starred = false) => new(
        item.ItemId, item.SpaceId, item.ParentId, item.ItemKindId, item.Name, item.OwnerUserId,
        item.CurrentVersionId, item.SizeBytes, item.BreakInheritance, starred,
        item.TrashedAt, item.CreatedAt, item.CreatedByUserId, item.UpdatedAt);
}

/// <summary>One immutable numbered version of a Drive file.</summary>
public sealed record DriveVersionResponse(
    Guid VersionId,
    Guid FileItemId,
    int VersionNumber,
    int StorageProviderId,
    long SizeBytes,
    string? ChecksumSha256,
    string MimeType,
    int SourceId,
    int StatusId,
    bool IsCurrent,
    Guid CreatedByUserId,
    DateTime CreatedAt,
    DateTime? CommittedAt)
{
    public static DriveVersionResponse From(DriveFileVersion version, Guid? currentVersionId) => new(
        version.VersionId, version.FileItemId, version.VersionNumber, version.StorageProviderId,
        version.SizeBytes, version.ChecksumSha256, version.MimeType, version.SourceId, version.StatusId,
        version.VersionId == currentVersionId, version.CreatedByUserId, version.CreatedAt, version.CommittedAt);
}

/// <summary>An explicit Drive grant (user or department) on an item.</summary>
public sealed record DrivePermissionResponse(
    Guid PermissionId,
    Guid ItemId,
    int PrincipalTypeId,
    Guid PrincipalId,
    int RoleId,
    Guid GrantedByUserId,
    DateTime GrantedAt)
{
    public static DrivePermissionResponse From(DrivePermission permission) => new(
        permission.PermissionId, permission.ItemId, permission.PrincipalTypeId, permission.PrincipalId,
        permission.RoleId, permission.GrantedByUserId, permission.GrantedAt);
}

/// <summary>A request to delete a Drive item, awaiting the owner's department head (or ADMIN).</summary>
public sealed record DriveDeletionRequestResponse(
    Guid RequestId,
    Guid ItemId,
    Guid RequestedByUserId,
    string Reason,
    int StatusId,
    Guid? ReviewedByUserId,
    DateTime? ReviewedAt,
    string? ReviewNote,
    DateTime CreatedAt)
{
    public static DriveDeletionRequestResponse From(DriveDeletionRequest request) => new(
        request.RequestId, request.ItemId, request.RequestedByUserId, request.Reason, request.StatusId,
        request.ReviewedByUserId, request.ReviewedAt, request.ReviewNote, request.CreatedAt);
}

/// <summary>The institution-wide Drive defaults (editable with DRIVE_MANAGE).</summary>
public sealed record DriveSettingsResponse(
    long DefaultQuotaBytes,
    long MaxFileSizeBytes,
    string AllowedExtensionsCsv)
{
    public static DriveSettingsResponse From(InstitutionSetting setting) => new(
        setting.DriveDefaultQuotaBytes, setting.DriveMaxFileSizeBytes, setting.DriveAllowedExtensionsCsv);
}

/// <summary>One ancestor in a breadcrumb path, root first.</summary>
public sealed record DriveBreadcrumbNode(Guid ItemId, string Name, int ItemKindId);

/// <summary>The reserved pending version and its short-lived direct-upload target.</summary>
public sealed record DriveUploadInitiatedResponse(
    Guid VersionId,
    Guid ItemId,
    int VersionNumber,
    string ObjectKey,
    Uri UploadUrl,
    DateTime ExpiresAtUtc);

/// <summary>A short-lived presigned download URL reachable by the browser and OnlyOffice.</summary>
public sealed record DriveDownloadUrlResponse(Uri Url, DateTime ExpiresAtUtc);

/// <summary>
/// A file's bytes for streaming. The controller writes <see cref="Content"/> with the given MIME type;
/// when a range was requested, <see cref="RangeStart"/>/<see cref="RangeEnd"/> are the inclusive bounds
/// actually served and <see cref="TotalSizeBytes"/> is the full object length (for Content-Range).
/// </summary>
public sealed record DriveFileContentResult(
    Stream Content,
    string MimeType,
    string FileName,
    long TotalSizeBytes,
    long? RangeStart = null,
    long? RangeEnd = null);