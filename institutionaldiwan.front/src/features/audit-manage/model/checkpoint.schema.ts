import { z } from 'zod'
import { t } from '@/shared/lib'

/** Blank seals through the latest entry; otherwise a positive audit-log id. */
export const checkpointSchema = z.object({
  toAuditLogId: z.string().trim().superRefine((value, ctx) => {
    if (!value) return
    if (!/^\d+$/.test(value) || Number(value) <= 0) {
      ctx.addIssue({
        code: 'custom',
        message: t('audit.validation.to-positive'),
      })
    }
  }),
})

export type CheckpointFormValues = z.infer<typeof checkpointSchema>
