/** `DriveItemResponse` — one unified folder/file node. `starred` is per-caller. */
export interface DriveItemResponse {
  itemId: string
  spaceId: string
  parentId: string | null
  /** `DRIVE_ITEM_KIND`: 1 folder, 2 file. */
  itemKindId: number
  name: string
  ownerUserId: string
  currentVersionId: string | null
  /** Current version's bytes for a file; 0 for a folder. */
  sizeBytes: number
  breakInheritance: boolean
  starred: boolean
  trashedAt: string | null
  createdAt: string
  createdByUserId: string
  updatedAt: string | null
}

/** `DriveVersionResponse` — one immutable numbered version of a file. */
export interface DriveVersionResponse {
  versionId: string
  fileItemId: string
  versionNumber: number
  storageProviderId: number
  sizeBytes: number
  checksumSha256: string | null
  mimeType: string
  /** `DRIVE_VERSION_SOURCE`: 1 upload, 2 editor save, 3 restore. */
  sourceId: number
  /** `DRIVE_VERSION_STATUS`: 1 pending, 2 committed, 3 abandoned. */
  statusId: number
  isCurrent: boolean
  createdByUserId: string
  createdAt: string
  committedAt: string | null
}

/** `DrivePermissionResponse` — one explicit grant (inherited grants are not listed). */
export interface DrivePermissionResponse {
  permissionId: string
  itemId: string
  /** `DRIVE_PRINCIPAL_TYPE`: 1 user, 2 department. */
  principalTypeId: number
  principalId: string
  /** `DRIVE_ROLE`: 1 viewer, 2 editor, 3 manager. */
  roleId: number
  grantedByUserId: string
  grantedAt: string
}

/** `DriveDeletionRequestResponse` — one approval-gated deletion request. */
export interface DriveDeletionRequestResponse {
  requestId: string
  itemId: string
  requestedByUserId: string
  reason: string
  /** `DRIVE_DELETION_STATUS`: 1 pending, 2 approved, 3 rejected, 4 cancelled. */
  statusId: number
  reviewedByUserId: string | null
  reviewedAt: string | null
  reviewNote: string | null
  createdAt: string
}

/** `DriveBreadcrumbNode` — one ancestor (root first, the item itself last). */
export interface DriveBreadcrumbNode {
  itemId: string
  name: string
  itemKindId: number
}

/** `DriveEffectiveRoleResponse` — the caller's effective role; 0 means no access. */
export interface DriveEffectiveRoleResponse {
  roleId: number
}

/** `DriveDownloadUrlResponse` — a short-lived presigned GET (5 min) for download/media/OnlyOffice. */
export interface DriveDownloadUrlResponse {
  url: string
  expiresAtUtc: string
}

/** `GET /api/drive/items` query — access-filtered, server-paged browse. */
export interface DriveBrowseParams {
  page?: number
  pageSize?: number
  spaceId?: string | null
  parentId?: string | null
  search?: string | null
  /** `DRIVE_ITEM_KIND`: 1 folder, 2 file. */
  kind?: number | null
  ownerUserId?: string | null
  trashed?: boolean
  starred?: boolean
  modifiedFrom?: string | null
  modifiedTo?: string | null
  /** `name` | `modifiedAt` | `size`. */
  sortBy?: string | null
  sortDescending?: boolean
}

/** `GET /api/drive/deletion-requests` query — visibility filtered server-side. */
export interface DriveDeletionRequestParams {
  page?: number
  pageSize?: number
  statusId?: number | null
  mineOnly?: boolean
}

/** `GET /api/drive/trash` query — newest trashed first. */
export interface DriveTrashParams {
  page?: number
  pageSize?: number
  spaceId?: string | null
  search?: string | null
}