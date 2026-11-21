/**
 * Module 11 Drive (الملفات) structural constants — a one-to-one mirror of
 * `InstitutionalDiwan.Domain/Entities/Drive/DriveCatalog.cs`. These are code contracts, not editable
 * lookups: the ids never change at runtime, and the Arabic wording lives in `labels/drive.ts`
 * (`drive.space-type.*`, `drive.item-kind.*`, `drive.role.*`, …). Data-driven text (space/folder/file
 * names, user/department names) stays out of the catalog.
 */

/** `DriveSpaceType` — a personal space per user plus freely created shared spaces. */
export const DRIVE_SPACE_TYPE = {
  Personal: 1,
  Shared: 2,
} as const

/** `DriveItemKind` — one unified node for folders and files. */
export const DRIVE_ITEM_KIND = {
  Folder: 1,
  File: 2,
} as const

/** `DriveRole` — effective access, ordered weakest to strongest; 0 means no access. */
export const DRIVE_ROLE = {
  None: 0,
  Viewer: 1,
  Editor: 2,
  Manager: 3,
} as const

/** `DrivePrincipalType` — a grant targets a user or a department. */
export const DRIVE_PRINCIPAL_TYPE = {
  User: 1,
  Department: 2,
} as const

/** `DriveVersionStatus` — Pending while a presigned upload is in flight, then Committed/Abandoned. */
export const DRIVE_VERSION_STATUS = {
  Pending: 1,
  Committed: 2,
  Abandoned: 3,
} as const

/** `DriveVersionSource` — how a committed version came to be. */
export const DRIVE_VERSION_SOURCE = {
  Upload: 1,
  EditorSave: 2,
  Restore: 3,
} as const

/** `DriveDeletionRequestStatus` — the approval-gated deletion lifecycle. */
export const DRIVE_DELETION_STATUS = {
  Pending: 1,
  Approved: 2,
  Rejected: 3,
  Cancelled: 4,
} as const

/** `DriveEditSessionStatus` — the OnlyOffice collaboration room state. */
export const DRIVE_SESSION_STATUS = {
  Open: 1,
  Saved: 2,
  Closed: 3,
  Expired: 4,
} as const

/** `FileStorageProvider.DriveSeaweedFsId` — the S3-compatible SeaweedFS gateway Drive stores bytes in. */
export const DRIVE_STORAGE_PROVIDER_ID = 2