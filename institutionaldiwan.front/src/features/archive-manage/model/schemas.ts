import { z } from 'zod'

const REASON_MAX = 500
const SEARCH_NAME_MAX = 150

/** Mirrors `ArchiveDocumentRequest`: the location is required, the reason optional. */
export const archiveDocumentSchema = z.object({
  archiveLocationId: z.string().min(1, 'اختر موقع الأرشفة.'),
  reason: z.string().trim().max(REASON_MAX, 'السبب يتجاوز 500 حرف.'),
})

export type ArchiveDocumentFormValues = z.infer<typeof archiveDocumentSchema>

/** Mirrors `UnarchiveDocumentRequest`: a reason is mandatory. */
export const unarchiveDocumentSchema = z.object({
  reason: z.string().trim().min(1, 'سبب إلغاء الأرشفة مطلوب.').max(REASON_MAX, 'السبب يتجاوز 500 حرف.'),
})

export type UnarchiveDocumentFormValues = z.infer<typeof unarchiveDocumentSchema>

/** The criteria are captured from the grid, so the form only asks for a name and the default flag. */
export const saveSearchSchema = z.object({
  name: z
    .string()
    .trim()
    .min(1, 'اسم البحث مطلوب.')
    .max(SEARCH_NAME_MAX, 'اسم البحث يتجاوز 150 حرفًا.'),
  isDefault: z.boolean(),
})

export type SaveSearchFormValues = z.infer<typeof saveSearchSchema>
