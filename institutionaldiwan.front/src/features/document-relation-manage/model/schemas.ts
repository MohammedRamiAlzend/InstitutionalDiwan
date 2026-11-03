import { z } from 'zod'

const TEXT_MAX = 500

/** Mirrors `CreateDocumentRelationRequest`: target and type are required, notes optional. */
export const createRelationSchema = z.object({
  targetDocumentId: z.string().min(1, 'اختر الوثيقة المراد ربطها.'),
  relationTypeId: z.string().min(1, 'اختر نوع الرابط.'),
  notes: z.string().trim().max(TEXT_MAX, 'الملاحظات تتجاوز 500 حرف.'),
})

export type CreateRelationFormValues = z.infer<typeof createRelationSchema>

/** Mirrors `RemoveDocumentRelationRequest`: a reason is mandatory. */
export const removeRelationSchema = z.object({
  reason: z.string().trim().min(1, 'سبب إزالة الرابط مطلوب.').max(TEXT_MAX, 'السبب يتجاوز 500 حرف.'),
})

export type RemoveRelationFormValues = z.infer<typeof removeRelationSchema>
