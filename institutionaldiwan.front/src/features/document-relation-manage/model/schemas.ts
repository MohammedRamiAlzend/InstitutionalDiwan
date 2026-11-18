import { z } from 'zod'
import { t } from '@/shared/lib'

const TEXT_MAX = 500

/** Mirrors `CreateDocumentRelationRequest`: target and type are required, notes optional. */
export const createRelationSchema = z.object({
  targetDocumentId: z.string().min(1, t('relation.validation.target-required')),
  relationTypeId: z.string().min(1, t('relation.validation.type-required')),
  notes: z.string().trim().max(TEXT_MAX, t('relation.validation.notes-max')),
})

export type CreateRelationFormValues = z.infer<typeof createRelationSchema>

/** Mirrors `RemoveDocumentRelationRequest`: a reason is mandatory. */
export const removeRelationSchema = z.object({
  reason: z.string().trim().min(1, t('relation.validation.reason-required')).max(TEXT_MAX, t('relation.validation.reason-max')),
})

export type RemoveRelationFormValues = z.infer<typeof removeRelationSchema>