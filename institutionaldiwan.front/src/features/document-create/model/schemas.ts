import { z } from 'zod'

const MIN_NUMBERING_YEAR = 1900
const MAX_NUMBERING_YEAR = 2200

const documentFields = {
  title: z
    .string()
    .trim()
    .min(1, 'عنوان الوثيقة مطلوب.')
    .max(500, 'عنوان الوثيقة يتجاوز الحد الأقصى للأحرف.'),
  documentTypeId: z.string().min(1, 'اختر نوع الوثيقة.'),
  directionId: z.string().min(1, 'اختر اتجاه الوثيقة.'),
  subjectSummary: z
    .string()
    .trim()
    .max(2000, 'ملخص الموضوع يتجاوز الحد الأقصى للأحرف.'),
  confidentialityLevelId: z.string().min(1, 'اختر مستوى السرية.'),
  priorityLevelId: z.string().min(1, 'اختر درجة الأولوية.'),
  officialNumberText: z
    .string()
    .trim()
    .max(100, 'الرقم الرسمي يتجاوز الحد الأقصى للأحرف.'),
  numberingYear: z.string(),
  documentDate: z.string(),
  externalReferenceNumber: z
    .string()
    .trim()
    .max(100, 'الرقم المرجعي الخارجي يتجاوز الحد الأقصى للأحرف.'),
  externalDocumentDate: z.string(),
  mode: z.enum(['draft', 'register']),
}

/** Shared form for both `POST /api/documents` and `POST /api/documents/drafts`. */
export const createDocumentSchema = z.object(documentFields).superRefine((values, ctx) => {
  if (values.mode !== 'register') return

  if (!values.officialNumberText) {
    ctx.addIssue({
      code: 'custom',
      path: ['officialNumberText'],
      message: 'الرقم الرسمي مطلوب عند التسجيل الفوري.',
    })
  }

  const year = Number(values.numberingYear)
  if (!values.numberingYear.trim()) {
    ctx.addIssue({
      code: 'custom',
      path: ['numberingYear'],
      message: 'سنة الترقيم مطلوبة.',
    })
  } else if (
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

export type CreateDocumentFormValues = z.infer<typeof createDocumentSchema>
export type CreateDocumentMode = CreateDocumentFormValues['mode']
