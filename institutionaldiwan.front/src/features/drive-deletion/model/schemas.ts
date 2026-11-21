import { z } from 'zod'

export const deletionReasonSchema = z.object({
  reason: z.string().trim().min(1, 'سبب الحذف مطلوب.').max(500, 'السبب يتجاوز 500 حرف.'),
})

export const reviewNoteSchema = z.object({
  note: z.string().trim().max(500, 'ملاحظة المراجعة تتجاوز 500 حرف.'),
})

export type DeletionReasonFormValues = z.infer<typeof deletionReasonSchema>
export type ReviewNoteFormValues = z.infer<typeof reviewNoteSchema>
