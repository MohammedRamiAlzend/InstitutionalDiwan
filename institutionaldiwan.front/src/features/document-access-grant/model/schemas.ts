import { z } from 'zod'
import { t } from '@/shared/lib'

/** Local `yyyy-MM-dd` for the date input; avoids locale-dependent formatting. */
function todayIsoDate(): string {
  const now = new Date()
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  return `${now.getFullYear()}-${month}-${day}`
}

/** The expiry is optional; the server rejects any value that is not in the future. */
export const grantAccessSchema = z
  .object({
    userId: z.string().min(1, t('docdetails.validation.user-required')),
    accessLevelId: z.string().min(1, t('docdetails.validation.level-required')),
    reason: z.string().trim().max(500, t('docdetails.validation.reason-max')),
    expiresAt: z.string(),
  })
  .superRefine((values, ctx) => {
    if (!values.expiresAt) return

    if (!/^\d{4}-\d{2}-\d{2}$/.test(values.expiresAt)) {
      ctx.addIssue({
        code: 'custom',
        path: ['expiresAt'],
        message: t('docdetails.validation.expiry-invalid'),
      })
      return
    }

    if (values.expiresAt <= todayIsoDate()) {
      ctx.addIssue({
        code: 'custom',
        path: ['expiresAt'],
        message: t('docdetails.validation.expiry-future'),
      })
    }
  })

export type GrantAccessFormValues = z.infer<typeof grantAccessSchema>
