/** `DocumentFileVersionResponse` — one immutable numbered version of a document file. */
export interface DocumentFileVersionResponse {
  versionId: string
  versionNumber: number
  storageProviderId: number
  /** Internal storage path. Never rendered: it exposes the server layout to any `DOCUMENT_VIEW` caller. */
  relativePath: string
  originalFileName: string
  mimeType: string
  fileSizeBytes: number
  checksumSha256: string
  isCurrent: boolean
  uploadedByUserId: string
  changeNotes: string | null
  uploadedAt: string
}

/** `DocumentFileResponse` — a document file with its full version history. */
export interface DocumentFileResponse {
  fileId: string
  documentId: string
  fileTypeId: number
  title: string
  isPrimaryScan: boolean
  currentVersionNumber: number
  isArchived: boolean
  createdByUserId: string
  createdAt: string
  updatedAt: string | null
  /** Descending by `versionNumber`, so `[0]` is the current version. */
  versions: DocumentFileVersionResponse[]
}

/** `AttachmentChecksumResponse` — a stored checksum against one recomputed from the bytes on disk. */
export interface AttachmentChecksumResponse {
  fileId: string
  versionId: string
  versionNumber: number
  recordedChecksumSha256: string
  computedChecksumSha256: string
  isMatch: boolean
}
