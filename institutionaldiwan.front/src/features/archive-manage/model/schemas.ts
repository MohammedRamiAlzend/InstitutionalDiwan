import { z } from 'zod'
import { t } from '@/shared/lib'

const REASON_MAX = 500
const SEARCH_NAME_MAX = 150

/** Mirrors `ArchiveDocumentRequest`: the location is required, the reason optional. */
export const archiveDocumentSchema = z.object({
  archiveLocationId: z.string().min(1, t('archive.archive.validation.location-required')),
  reason: z.string().trim().max(REASON_MAX, t('archive.archive.validation.reason-max')),
})

export type ArchiveDocumentFormValues = z.infer<typeof archiveDocumentSchema>

/** Mirrors `UnarchiveDocumentRequest`: a reason is mandatory. */
export const unarchiveDocumentSchema = z.object({
  reason: z.string().trim().min(1, t('archive.unarchive.validation.reason-required')).max(REASON_MAX, t('archive.unarchive.validation.reason-max')),
})

export type UnarchiveDocumentFormValues = z.infer<typeof unarchiveDocumentSchema>

/** The criteria are captured from the grid, so the form only asks for a name and the default flag. */
export const saveSearchSchema = z.object({
  name: z
    .string()
    .trim()
    .min(1, t('archive.save.validation.name-required'))
    .max(SEARCH_NAME_MAX, t('archive.save.validation.name-max')),
  isDefault: z.boolean(),
})

export type SaveSearchFormValues = z.infer<typeof saveSearchSchema>
