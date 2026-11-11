import { z } from 'zod'
import { DECISION_EFFECT } from '@/shared/config'

export const decisionTypeSchema = z
  .object({
    nameAr: z.string().trim().min(1, 'الاسم مطلوب.').max(200, 'الاسم لا يتجاوز 200 حرف.'),
    isAppealable: z.boolean(),
    effectId: z.string().min(1, 'اختر أثر القرار.'),
    destinationDepartmentId: z.string(),
    documentTypeIds: z.array(z.number().int().positive()).min(1, 'اختر نوع وثيقة واحدًا على الأقل.'),
  })
  .superRefine((value, context) => {
    const effect = Number(value.effectId)
    if (effect !== DECISION_EFFECT.Refer && value.destinationDepartmentId) {
      context.addIssue({
        code: 'custom',
        path: ['destinationDepartmentId'],
        message: 'قسم الوجهة يُحدَّد لنوع الإحالة فقط.',
      })
    }
  })

export type DecisionTypeFormValues = z.infer<typeof decisionTypeSchema>
