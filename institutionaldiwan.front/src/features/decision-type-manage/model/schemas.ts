import { z } from 'zod'
import { DECISION_EFFECT } from '@/shared/config'
import { t } from '@/shared/lib'

export const decisionTypeSchema = z
  .object({
    nameAr: z.string().trim().min(1, t('decisions.type.validation.name-required')).max(200, t('decisions.type.validation.name-max')),
    isAppealable: z.boolean(),
    effectId: z.string().min(1, t('decisions.type.validation.effect-required')),
    destinationDepartmentId: z.string(),
    documentTypeIds: z.array(z.number().int().positive()).min(1, t('decisions.type.validation.document-types-required')),
  })
  .superRefine((value, context) => {
    const effect = Number(value.effectId)
    if (effect !== DECISION_EFFECT.Refer && value.destinationDepartmentId) {
      context.addIssue({
        code: 'custom',
        path: ['destinationDepartmentId'],
        message: t('decisions.type.validation.destination-referral-only'),
      })
    }
  })

export type DecisionTypeFormValues = z.infer<typeof decisionTypeSchema>
