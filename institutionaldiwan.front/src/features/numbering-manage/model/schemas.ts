import { z } from 'zod'
import { unknownPatternTokens } from '@/entities/numbering-registry'

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
      message: `سنة الترقيم يجب أن تكون بين ${MIN_NUMBERING_YEAR} و${MAX_NUMBERING_YEAR}.`,
    })
  }
})

/** Create/edit registry. `formatPattern` keeps the default when left blank. */
export const registryFormSchema = z.object({
  code: z
    .string()
    .trim()
    .min(1, 'رمز سجل الترقيم مطلوب.')
    .max(50, 'رمز سجل الترقيم يتجاوز الحد الأقصى للأحرف.')
    .regex(
      /^[A-Za-z0-9][A-Za-z0-9_-]*$/,
      'الرمز يبدأ بحرف أو رقم ويقبل الأحرف والأرقام والشرطة فقط.',
    ),
  nameAr: z
    .string()
    .trim()
    .min(1, 'اسم سجل الترقيم مطلوب.')
    .max(200, 'اسم سجل الترقيم يتجاوز الحد الأقصى للأحرف.'),
  formatPattern: z
    .string()
    .trim()
    .max(100, 'قالب الرقم يتجاوز الحد الأقصى للأحرف.')
    .superRefine((value, ctx) => {
      if (!value) return

      const unknown = unknownPatternTokens(value)
      if (unknown.length > 0) {
        ctx.addIssue({
          code: 'custom',
          message: `رموز غير معروفة في القالب: ${unknown.join('، ')}. المتاح {YEAR} و{SEQ} أو {SEQ:D6}.`,
        })
        return
      }

      if (!value.toUpperCase().includes('{SEQ')) {
        ctx.addIssue({
          code: 'custom',
          message: 'يجب أن يتضمن القالب الرمز {SEQ} حتى لا تتكرر الأرقام.',
        })
      }
    }),
  resetPolicyId: z.string().min(1, 'اختر سياسة التصفير.'),
  departmentId: z.string(),
  documentTypeId: z.string(),
  directionId: z.string(),
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
    .min(1, 'سبب الإلغاء مطلوب.')
    .max(1000, 'السبب يتجاوز الحد الأقصى للأحرف.'),
})

export type CancelNumberFormValues = z.infer<typeof cancelNumberSchema>

export const replaceNumberSchema = z.object({
  reason: z
    .string()
    .trim()
    .min(1, 'سبب الاستبدال مطلوب.')
    .max(1000, 'السبب يتجاوز الحد الأقصى للأحرف.'),
  numberingYear: optionalNumberingYear,
})

export type ReplaceNumberFormValues = z.infer<typeof replaceNumberSchema>

export const departmentRegistrationSchema = z
  .object({
    departmentId: z.string().min(1, 'اختر الدائرة.'),
    registryNumberText: z
      .string()
      .trim()
      .min(1, 'رقم تسجيل الدائرة مطلوب.')
      .max(100, 'رقم تسجيل الدائرة يتجاوز الحد الأقصى للأحرف.'),
    registryDate: z.string().min(1, 'تاريخ تسجيل الدائرة مطلوب.'),
    notes: z.string().trim().max(1000, 'الملاحظات تتجاوز الحد الأقصى للأحرف.'),
  })
  .superRefine((values, ctx) => {
    if (!values.registryDate) return

    if (!/^\d{4}-\d{2}-\d{2}$/.test(values.registryDate)) {
      ctx.addIssue({
        code: 'custom',
        path: ['registryDate'],
        message: 'تاريخ تسجيل الدائرة غير صالح.',
      })
      return
    }

    const year = Number(values.registryDate.slice(0, 4))
    if (year < MIN_NUMBERING_YEAR || year > MAX_NUMBERING_YEAR) {
      ctx.addIssue({
        code: 'custom',
        path: ['registryDate'],
        message: `سنة التسجيل يجب أن تكون بين ${MIN_NUMBERING_YEAR} و${MAX_NUMBERING_YEAR}.`,
      })
      return
    }

    if (values.registryDate > todayIsoDate()) {
      ctx.addIssue({
        code: 'custom',
        path: ['registryDate'],
        message: 'تاريخ تسجيل الدائرة لا يمكن أن يكون في المستقبل.',
      })
    }
  })

export type DepartmentRegistrationFormValues = z.infer<
  typeof departmentRegistrationSchema
>
