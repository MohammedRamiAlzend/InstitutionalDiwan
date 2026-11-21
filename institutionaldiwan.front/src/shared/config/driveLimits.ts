import { formatFileSize, t } from '@/shared/lib'

/**
 * Mirror of the institution Drive defaults seeded in
 * `InstitutionalDiwan.Domain/Entities/Organization/InstitutionSetting.cs:43-46`
 * (`DefaultDriveQuotaBytes` = 5 GB, `DefaultDriveMaxFileSizeBytes` = 200 MB,
 * `DefaultDriveAllowedExtensionsCsv`). `GET /api/drive/settings` is `DRIVE_MANAGE`-only, so a normal
 * user cannot read the live limits; the client keeps this copy to pre-check a pick before uploading.
 * The server re-checks every rule at `initiate`/`complete` and stays authoritative: if an admin
 * changes the defaults, the upload is rejected there and the Arabic `Drive.*` message shows. An admin
 * surface (Phase 5) reads the live settings instead of this mirror.
 */
export const DEFAULT_DRIVE_QUOTA_BYTES = 5 * 1024 * 1024 * 1024
export const DEFAULT_DRIVE_MAX_FILE_BYTES = 200 * 1024 * 1024

/** Lowercase extensions including the leading dot, exactly as the server's default CSV. */
export const DEFAULT_DRIVE_ALLOWED_EXTENSIONS: readonly string[] = [
  '.doc', '.docx', '.xls', '.xlsx', '.ppt', '.pptx', '.pdf', '.txt', '.md', '.csv',
  '.jpg', '.jpeg', '.png', '.gif', '.webp', '.svg',
  '.mp4', '.webm', '.mkv', '.mov', '.mp3', '.wav',
  '.zip', '.7z', '.rar',
]

/**
 * A pre-check rejection names the `Drive.*` code the server returns for the same case, so when the
 * mirror drifts from the live settings the user sees identical wording coming back from the server.
 */
function withCode(message: string, code: string): string {
  return `${message} (${code})`
}

/** Lowercase extension with the leading dot, or `''` for a name without one. */
function fileExtensionOf(fileName: string): string {
  const normalized = fileName.toLowerCase()
  const dotIndex = normalized.lastIndexOf('.')
  return dotIndex < 0 ? '' : normalized.slice(dotIndex)
}

/**
 * Every local rule a picked Drive file must pass before `initiate`. Returns the Arabic message to
 * show, or `null` when nothing local rejects it. `remainingQuotaBytes` (when known) adds the space's
 * remaining-quota check; the server re-checks quota at `initiate` and again at `complete` regardless.
 */
export function driveFileRejection(
  file: File | null,
  remainingQuotaBytes?: number | null,
): string | null {
  if (!file) return withCode(t('drive.validation.file-empty'), 'Drive.FileTooLarge')
  if (file.size === 0) return withCode(t('drive.validation.file-zero'), 'Drive.FileTooLarge')

  if (file.size > DEFAULT_DRIVE_MAX_FILE_BYTES) {
    return withCode(
      t('drive.validation.file-too-large', { size: formatFileSize(DEFAULT_DRIVE_MAX_FILE_BYTES) }),
      'Drive.FileTooLarge',
    )
  }

  const extension = fileExtensionOf(file.name)
  if (!DEFAULT_DRIVE_ALLOWED_EXTENSIONS.includes(extension)) {
    return withCode(
      extension
        ? t('drive.validation.extension-not-allowed', {
            extension,
            allowed: DEFAULT_DRIVE_ALLOWED_EXTENSIONS.join('، '),
          })
        : t('drive.validation.no-extension', {
            allowed: DEFAULT_DRIVE_ALLOWED_EXTENSIONS.join('، '),
          }),
      'Drive.ExtensionNotAllowed',
    )
  }

  if (
    remainingQuotaBytes !== null &&
    remainingQuotaBytes !== undefined &&
    file.size > remainingQuotaBytes
  ) {
    return withCode(
      t('drive.validation.quota-exceeded', { size: formatFileSize(remainingQuotaBytes) }),
      'Drive.QuotaExceeded',
    )
  }

  return null
}