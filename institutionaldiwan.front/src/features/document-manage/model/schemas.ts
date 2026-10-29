import { z } from 'zod'

const MIN_NUMBERING_YEAR = 1900
const MAX_NUMBERING_YEAR = 2200

/** Mirrors `UpdateDocumentMetadataRequest`; document type/direction stay immutable here. */
export const editDraftSchema = z.object({
  title: z
    .string()
    .trim()
    .min(1, 'عنوان الوثيقة مطلوب.')
    .max(500, 'عنوان الوثيقة يتجاوز الحد الأقصى للأحرف.'),
  subjectSummary: z
    .string()
    .trim()
    .max(2000, 'ملخص الموضوع يتجاوز الحد الأقصى للأحرف.'),
  confidentialityLevelId: z.string().min(1, 'اختر مستوى السرية.'),
  priorityLevelId: z.string().min(1, 'اختر درجة الأولوية.'),
})

export type EditDraftFormValues = z.infer<typeof editDraftSchema>

/** The year is optional: blank lets the server use the document year, then the current year. */
export const registerDraftSchema = z
  .object({ numberingYear: z.string() })
  .superRefine((values, ctx) => {
    if (!values.numberingYear.trim()) return

    const year = Number(values.numberingYear)
    if (
      !/^\d{4}$/.test(values.numberingYear.trim()) ||
      year < MIN_NUMBERING_YEAR ||
      year > MAX_NUMBERING_YEAR
    ) {
      ctx.addIssue({
        code: 'custom',
        path: ['numberingYear'],
        message: `سنة الترقيم يجب أن تكون بين ${MIN_NUMBERING_YEAR} و${MAX_NUMBERING_YEAR}.`,
      })
    }
  })

export type RegisterDraftFormValues = z.infer<typeof registerDraftSchema>

export const PARTICIPANT_TARGET_TYPES = ['external', 'department', 'person'] as const
export type ParticipantTargetKey = (typeof PARTICIPANT_TARGET_TYPES)[number]

/** The form picks one target kind; only its matching field is sent to the server. */
export const addParticipantSchema = z
  .object({
    roleId: z.string().min(1, 'اختر دور المشارك.'),
    targetType: z.enum(PARTICIPANT_TARGET_TYPES),
    externalOrganizationId: z.string(),
    internalDepartmentId: z.string(),
    personName: z
      .string()
      .trim()
      .max(200, 'اسم الشخص يتجاوز الحد الأقصى للأحرف.'),
    isPrimary: z.boolean(),
    notes: z.string().trim().max(1000, 'الملاحظات تتجاوز الحد الأقصى للأحرف.'),
  })
  .superRefine((values, ctx) => {
    if (values.targetType === 'external' && !values.externalOrganizationId) {
      ctx.addIssue({
        code: 'custom',
        path: ['externalOrganizationId'],
        message: 'اختر الجهة الخارجية.',
      })
    }
    if (values.targetType === 'department' && !values.internalDepartmentId) {
      ctx.addIssue({
        code: 'custom',
        path: ['internalDepartmentId'],
        message: 'اختر القسم المشارك.',
      })
    }
    if (values.targetType === 'person' && !values.personName.trim()) {
      ctx.addIssue({
        code: 'custom',
        path: ['personName'],
        message: 'اسم الشخص مطلوب.',
      })
    }
  })

export type AddParticipantFormValues = z.infer<typeof addParticipantSchema>

export const cancelDocumentSchema = z.object({
  reason: z
    .string()
    .trim()
    .min(1, 'سبب الإلغاء مطلوب.')
    .max(1000, 'سبب الإلغاء يتجاوز الحد الأقصى للأحرف.'),
})

export type CancelDocumentFormValues = z.infer<typeof cancelDocumentSchema>
