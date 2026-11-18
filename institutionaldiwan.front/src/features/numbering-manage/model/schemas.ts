import { z } from 'zod'
import { unknownPatternTokens } from '@/entities/numbering-registry'
import { t } from '@/shared/lib'

const MIN_NUMBERING_YEAR = 1900
const MAX_NUMBERING_YEAR = 2200

/** Local `yyyy-MM-dd` compared lexically against the date input value. */
function todayIsoDate(): string {
  const now = new Date()
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  return `${now.getFullYear()}-${month}-${day}`
}

/** Blank is allowed; otherwise a four-digit year inside the supported range. */
const optionalNumberingYear = z.string().superRefine((value, ctx) => {
  if (!value.trim()) return

  const year = Number(value)
  if (!/^\d{4}$/.test(value.trim()) || year < MIN_NUMBERING_YEAR || year > MAX_NUMBERING_YEAR) {
    ctx.addIssue({
      code: 'custom',
      message: t('numbering.validation.year-range', {
        min: MIN_NUMBERING_YEAR,
        max: MAX_NUMBERING_YEAR,
      }),
    })
  }
})

/** Create/edit registry. `formatPattern` keeps the default when left blank. */
export const registryFormSchema = z.object({
  code: z
    .string()
    .trim()
    .min(1, t('numbering.validation.code-required'))
    .max(50, t('numbering.validation.code-max'))
    .regex(
      /^[A-Za-z0-9][A-Za-z0-9_-]*$/,
      t('numbering.validation.code-format'),
    ),
  nameAr: z
    .string()
    .trim()
    .min(1, t('numbering.validation.name-required'))
    .max(200, t('numbering.validation.name-max')),
  formatPattern: z
    .string()
    .trim()
    .max(100, t('numbering.validation.pattern-max'))
    .superRefine((value, ctx) => {
      if (!value) return

      const unknown = unknownPatternTokens(value)
      if (unknown.length > 0) {
        ctx.addIssue({
          code: 'custom',
          message: t('numbering.validation.pattern-unknown', { tokens: unknown.join('، ') }),
        })
        return
      }

      if (!value.toUpperCase().includes('{SEQ')) {
        ctx.addIssue({
          code: 'custom',
          message: t('numbering.validation.pattern-seq-required'),
        })
      }
    }),
  resetPolicyId: z.string().min(1, t('numbering.validation.reset-policy-required')),
  departmentId: z.string(),
  documentTypeId: z.string(),
})

export type RegistryFormValues = z.infer<typeof registryFormSchema>

export const issueNumberSchema = z.object({
  numberingYear: optionalNumberingYear,
})

export type IssueNumberFormValues = z.infer<typeof issueNumberSchema>

export const cancelNumberSchema = z.object({
  reason: z
    .string()
    .trim()
    .min(1, t('numbering.validation.cancel-reason-required'))
    .max(1000, t('numbering.validation.reason-max')),
})

export type CancelNumberFormValues = z.infer<typeof cancelNumberSchema>

export const replaceNumberSchema = z.object({
  reason: z
    .string()
    .trim()
    .min(1, t('numbering.validation.replace-reason-required'))
    .max(1000, t('numbering.validation.reason-max')),
  numberingYear: optionalNumberingYear,
})

export type ReplaceNumberFormValues = z.infer<typeof replaceNumberSchema>

export const departmentRegistrationSchema = z
  .object({
    departmentId: z.string().min(1, t('numbering.validation.department-required')),
    registryId: z.string(),
    registryNumberText: z
      .string()
      .trim()
      .min(1, t('numbering.validation.reg-number-required'))
      .max(100, t('numbering.validation.reg-number-max')),
    registryDate: z.string().min(1, t('numbering.validation.reg-date-required')),
    notes: z.string().trim().max(1000, t('numbering.validation.notes-max')),
  })
  .superRefine((values, ctx) => {
    if (!values.registryDate) return

    if (!/^\d{4}-\d{2}-\d{2}$/.test(values.registryDate)) {
      ctx.addIssue({
        code: 'custom',
        path: ['registryDate'],
        message: t('numbering.validation.reg-date-invalid'),
      })
      return
    }

    const year = Number(values.registryDate.slice(0, 4))
    if (year < MIN_NUMBERING_YEAR || year > MAX_NUMBERING_YEAR) {
      ctx.addIssue({
        code: 'custom',
        path: ['registryDate'],
        message: t('numbering.validation.reg-year-range', {
          min: MIN_NUMBERING_YEAR,
          max: MAX_NUMBERING_YEAR,
        }),
      })
      return
    }

    if (values.registryDate > todayIsoDate()) {
      ctx.addIssue({
        code: 'custom',
        path: ['registryDate'],
        message: t('numbering.validation.reg-date-future'),
      })
    }
  })

export type DepartmentRegistrationFormValues = z.infer<
  typeof departmentRegistrationSchema
>