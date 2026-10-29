import { z } from 'zod'

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
    startAuditLogId: positiveAuditLogId('معرّف بداية النطاق يجب أن يكون رقمًا صحيحًا موجبًا.'),
    endAuditLogId: positiveAuditLogId('معرّف نهاية النطاق يجب أن يكون رقمًا صحيحًا موجبًا.'),
  })
  .superRefine((values, ctx) => {
    if (!values.startAuditLogId || !values.endAuditLogId) return
    if (Number(values.endAuditLogId) < Number(values.startAuditLogId)) {
      ctx.addIssue({
        code: 'custom',
        path: ['endAuditLogId'],
        message: 'نهاية النطاق لا يمكن أن تسبق بدايته.',
      })
    }
  })

export type IntegrityRangeFormValues = z.infer<typeof integrityRangeSchema>
