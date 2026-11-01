import { DOCUMENT_FILE_TYPE } from './lookupIds'

/** Upload constraints the server enforces for one `DocumentFileType`. */
export interface FileTypeUploadLimits {
  maxBytes: number
  /** Lowercase extensions including the leading dot, as stored in `AllowedExtensionsCsv`. */
  allowedExtensions: readonly string[]
}

const GENERAL_EXTENSIONS = [
  '.pdf',
  '.doc',
  '.docx',
  '.xls',
  '.xlsx',
  '.jpg',
  '.jpeg',
  '.png',
  '.tif',
  '.tiff',
  '.zip',
] as const

/**
 * Mirror of `DocumentFileTypeSeeder.BuildItem`
 * (`InstitutionalDiwan.Infrastructure/Persistence/Seed/AttachmentsLookupSeeders.cs:37-45`).
 * `/api/lookups/document-file-types` returns ids and labels only, so the client keeps a copy to
 * validate a pick before sending it. The server re-checks every rule and stays authoritative: if
 * these values drift, the upload is rejected there and the Arabic `Attachments.*` message shows.
 */
export const DOCUMENT_FILE_TYPE_LIMITS: Readonly<Record<number, FileTypeUploadLimits>> = {
  [DOCUMENT_FILE_TYPE.Scan]: {
    maxBytes: 20 * 1024 * 1024,
    allowedExtensions: ['.pdf', '.jpg', '.jpeg', '.png', '.tif', '.tiff'],
  },
  [DOCUMENT_FILE_TYPE.Attachment]: {
    maxBytes: 50 * 1024 * 1024,
    allowedExtensions: GENERAL_EXTENSIONS,
  },
  [DOCUMENT_FILE_TYPE.Signature]: {
    maxBytes: 2 * 1024 * 1024,
    allowedExtensions: ['.png', '.jpg', '.jpeg', '.svg'],
  },
  [DOCUMENT_FILE_TYPE.Other]: {
    maxBytes: 50 * 1024 * 1024,
    allowedExtensions: GENERAL_EXTENSIONS,
  },
}

/** `DocumentFilesController.MaxUploadBytes`: 50 MB of file plus 1 MB of multipart overhead. */
export const MAX_UPLOAD_BYTES = 51 * 1024 * 1024

/**
 * Limits for one file type, or `null` when the server seeded a type this client does not know.
 * `null` means "no client-side pre-check" rather than "strictest": guessing a limit for an unknown
 * type would hard-block an upload the server would have accepted, so the server decides.
 */
export function fileTypeLimits(
  fileTypeId: number | null | undefined,
): FileTypeUploadLimits | null {
  if (fileTypeId === null || fileTypeId === undefined) return null
  return DOCUMENT_FILE_TYPE_LIMITS[fileTypeId] ?? null
}
