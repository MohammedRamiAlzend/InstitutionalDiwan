import type { DriveVersionResponse } from '@/entities/drive-item'

/** `POST /api/drive/files/initiate` — reserves a pending version and signs a direct PUT. */
export interface InitiateDriveUploadRequest {
  spaceId: string
  parentId?: string | null
  fileName: string
  declaredSizeBytes: number
  /** Must match the `Content-Type` sent on the presigned PUT (the signature covers it). */
  mimeType?: string | null
  /** When set, adds a version to this existing file instead of creating a new one. */
  fileItemId?: string | null
}

/** `DriveUploadInitiatedResponse` — the reserved version and its short-lived upload target. */
export interface DriveUploadInitiatedResponse {
  versionId: string
  itemId: string
  versionNumber: number
  objectKey: string
  uploadUrl: string
  expiresAtUtc: string
}

/** `POST /api/drive/files/{versionId}/complete` — optional client SHA-256 to verify the transfer. */
export interface CompleteDriveUploadRequest {
  checksumSha256?: string | null
}

/** Where an upload lands: a space + optional folder, or an existing file (new version). */
export interface DriveUploadTarget {
  spaceId: string
  parentId?: string | null
  /** Set for a new-version upload; the dialog then accepts a single file. */
  fileItemId?: string | null
  fileName?: string | null
}

export type { DriveVersionResponse }