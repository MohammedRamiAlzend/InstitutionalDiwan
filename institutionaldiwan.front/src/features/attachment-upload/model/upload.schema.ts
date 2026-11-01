import { z } from 'zod'
import { fileTypeLimits, MAX_UPLOAD_BYTES } from '@/shared/config'
import { formatFileSize } from '@/shared/lib'

/**
 * A pre-check rejection is the client's guess at a rule the server enforces anyway, so each message
 * names the `Attachments.*` code the server returns for the same case. When the mirror drifts from
 * the seed data the user sees the identical wording coming back from the server.
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
 * Every local rule a picked file must pass — the same rules for a first version and for a newer one
 * because both endpoints share the request cap and the type's limits. Returns the Arabic message to
 * show, or `null` when nothing local rejects the file; the server stays the last judge either way.
 */
export function fileRejection(file: File | null, fileTypeId: number | null): string | null {
  if (!file) return withCode('اختر ملفًا للرفع.', 'Attachments.FileEmpty')
  if (file.size === 0) return withCode('الملف المرفوع فارغ.', 'Attachments.FileEmpty')

  if (file.size > MAX_UPLOAD_BYTES) {
    // The request itself is capped in the server, so this rejection is not about the file type.
    return withCode(
      `حجم الملف يتجاوز الحد الأقصى للطلب (${formatFileSize(MAX_UPLOAD_BYTES)}).`,
      'Attachments.FileTooLarge',
    )
  }

  // `null` limits mean the server seeded a type this client does not know: no pre-check, the server decides.
  const limits = fileTypeId === null ? null : fileTypeLimits(fileTypeId)
  if (!limits) return null

  const extension = fileExtensionOf(file.name)
  if (!limits.allowedExtensions.includes(extension)) {
    return withCode(
      extension
        ? `الامتداد ${extension} غير مسموح لنوع الملف المحدد؛ المسموح ${limits.allowedExtensions.join('، ')}.`
        : `الملف بلا امتداد، والمسموح لنوع الملف المحدد ${limits.allowedExtensions.join('، ')}.`,
      'Attachments.ExtensionNotAllowed',
    )
  }

  if (file.size > limits.maxBytes) {
    // Only the limit is quoted: `formatFileSize` rounds, so a file one byte over 20 MB would read as
    // «20 م.ب يتجاوز 20 م.ب».
    return withCode(
      `حجم الملف يتجاوز الحد الأقصى لنوعه (${formatFileSize(limits.maxBytes)}).`,
      'Attachments.FileTooLarge',
    )
  }

  return null
}

/** Optional free text, capped where the server caps it. */
const changeNotesField = z
  .string()
  .trim()
  .max(500, withCode('ملاحظات التغيير يجب ألا تتجاوز 500 حرف.', 'Attachments.ChangeNotesTooLong'))

/**
 * Nullable, not required-with-message: an unpicked file arrives as `null` here and is reported by the
 * same check that covers size and extension, so one field never has two error owners.
 */
const fileField = z.file().nullable()

const fields = {
  fileTypeId: z.string().min(1, withCode('اختر نوع الملف.', 'Attachments.FileTypeEmpty')),
  title: z
    .string()
    .trim()
    .min(1, withCode('عنوان الملف مطلوب.', 'Attachments.TitleEmpty'))
    .max(300, withCode('عنوان الملف يجب ألا يتجاوز 300 حرف.', 'Attachments.TitleTooLong')),
  isPrimaryScan: z.boolean(),
  changeNotes: changeNotesField,
  file: fileField,
}

export const uploadAttachmentSchema = z.object(fields).superRefine((values, ctx) => {
  const message = fileRejection(values.file, Number(values.fileTypeId))
  if (message) ctx.addIssue({ code: 'custom', path: ['file'], message })
})

export type UploadAttachmentFormValues = z.infer<typeof uploadAttachmentSchema>

/**
 * A new version keeps the file's own type and title, so only the bytes and the notes are validated.
 * The type is a parameter, not a form field, because the user never chooses it here.
 */
export function buildUploadVersionSchema(fileTypeId: number) {
  return z
    .object({ file: fileField, changeNotes: changeNotesField })
    .superRefine((values, ctx) => {
      const message = fileRejection(values.file, fileTypeId)
      if (message) ctx.addIssue({ code: 'custom', path: ['file'], message })
    })
}

export type UploadVersionFormValues = z.infer<ReturnType<typeof buildUploadVersionSchema>>
