import { z } from 'zod'
import { t } from '@/shared/lib'

const positiveAuditLogId = (message: string) =>
  z.string().trim().superRefine((value, ctx) => {
    if (!value) return
    if (!/^\d+$/.test(value) || Number(value) <= 0) {
      ctx.addIssue({ code: 'custom', message })
    }
  })

/** Both ends are optional: an empty range verifies the whole chain. */
export const integrityRangeSchema = z
  .object({
    startAuditLogId: positiveAuditLogId(t('audit.validation.start-positive')),
    endAuditLogId: positiveAuditLogId(t('audit.validation.end-positive')),
  })
  .superRefine((values, ctx) => {
    if (!values.startAuditLogId || !values.endAuditLogId) return
    if (Number(values.endAuditLogId) < Number(values.startAuditLogId)) {
      ctx.addIssue({
        code: 'custom',
        path: ['endAuditLogId'],
        message: t('audit.validation.end-before-start'),
      })
    }
  })

export type IntegrityRangeFormValues = z.infer<typeof integrityRangeSchema>
