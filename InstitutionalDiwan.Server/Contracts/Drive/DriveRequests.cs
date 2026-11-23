namespace InstitutionalDiwan.Server.Contracts.Drive;

/// <summary>Payload for <c>POST /api/drive/spaces</c> — creates a shared space owned by the caller.</summary>
public sealed record CreateDriveSpaceRequest(string Name, long? QuotaBytes = null);

/// <summary>Payload for <c>PUT /api/drive/spaces/{id}</c> — renames a space (owner or ADMIN).</summary>
public sealed record RenameDriveSpaceRequest(string Name);

/// <summary>Payload for <c>PUT /api/drive/spaces/{id}/quota</c> — sets a per-space quota override (ADMIN). Null restores the institution default.</summary>
public sealed record SetDriveSpaceQuotaRequest(long? QuotaBytes);

/// <summary>Payload for <c>POST /api/drive/items/folders</c> — creates a folder in a space.</summary>
public sealed record CreateDriveFolderRequest(Guid SpaceId, Guid? ParentId, string Name);

/// <summary>Payload for <c>PUT /api/drive/items/{id}/rename</c>.</summary>
public sealed record RenameDriveItemRequest(string Name);

/// <summary>Payload for <c>PUT /api/drive/items/{id}/move</c>. Null moves the item to its space root.</summary>
public sealed record MoveDriveItemRequest(Guid? NewParentId);

/// <summary>Payload for <c>POST /api/drive/files/initiate</c> — reserves a pending version and returns a presigned PUT URL.</summary>
public sealed record InitiateDriveUploadRequest(
    Guid SpaceId,
    Guid? ParentId,
    string FileName,
    long DeclaredSizeBytes,
    string? MimeType = null,
    Guid? FileItemId = null);

/// <summary>Payload for <c>POST /api/drive/files</c> — creates an empty Word/Excel/text/Markdown file.</summary>
public sealed record CreateDriveFileRequest(Guid SpaceId, Guid? ParentId, string FileName);

/// <summary>Payload for <c>PUT /api/drive/files/{itemId}/text</c> — the full new text content.</summary>
public sealed record SaveDriveTextContentRequest(string Content);

/// <summary>Payload for <c>POST /api/drive/files/{versionId}/complete</c> — optional client SHA-256 to verify against the stored object.</summary>
public sealed record CompleteDriveUploadRequest(string? ChecksumSha256 = null);

/// <summary>Payload for <c>POST /api/drive/items/{id}/permissions</c> — grants a user or department a role on an item.</summary>
public sealed record GrantDrivePermissionRequest(int PrincipalTypeId, Guid PrincipalId, int RoleId);

/// <summary>Payload for <c>PUT /api/drive/items/{id}/inheritance</c> — breaks or restores permission inheritance.</summary>
public sealed record SetDriveItemInheritanceRequest(bool BreakInheritance);

/// <summary>Payload for <c>POST /api/drive/items/{id}/deletion-requests</c> — requests approval-gated deletion.</summary>
public sealed record RequestDriveItemDeletionRequest(string Reason);

/// <summary>Payload for <c>POST /api/drive/deletion-requests/{reqId}/approve|reject</c> — optional reviewer note.</summary>
public sealed record ReviewDriveDeletionRequest(string? Note = null);

/// <summary>Payload for <c>PUT /api/drive/settings</c> — institution-wide Drive defaults (ADMIN).</summary>
public sealed record UpdateDriveSettingsRequest(
    long DefaultQuotaBytes,
    long MaxFileSizeBytes,
    string? AllowedExtensionsCsv);

/// <summary>
/// Body OnlyOffice posts to <c>POST /api/drive/onlyoffice/callback</c>. <c>status</c> is 1 (editing),
/// 2 (ready to save), 4 (closed without changes) or 6 (forcesave); <c>url</c> is the temporary download
/// URL of the saved document; <c>token</c> is the HS256 JWT signed with the shared secret.
/// </summary>
public sealed record DriveOnlyOfficeCallbackRequest(
    string? Key,
    int Status,
    string? Url,
    IReadOnlyList<string>? Users,
    int? ForceSaveType,
    string? Token);