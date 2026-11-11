import { z } from 'zod'

const DATE_PATTERN = /^\d{4}-\d{2}-\d{2}$/

export const issueDecisionSchema = z.object({
  decisionTypeId: z.string().min(1, 'اختر نوع القرار.'),
  decisionNumber: z.string().trim().min(1, 'رقم القرار مطلوب.').max(100, 'رقم القرار لا يتجاوز 100 حرف.'),
  decisionDate: z.string().regex(DATE_PATTERN, 'تاريخ القرار مطلوب.'),
  summary: z.string().trim().max(2000, 'الخلاصة لا تتجاوز 2000 حرف.'),
  documentFileId: z.string(),
  joinTargetDocumentId: z.string(),
})

export type IssueDecisionFormValues = z.infer<typeof issueDecisionSchema>

export const cancelDecisionSchema = z.object({
  reason: z.string().trim().min(1, 'سبب الإلغاء مطلوب.').max(1000, 'السبب لا يتجاوز 1000 حرف.'),
})

export type CancelDecisionFormValues = z.infer<typeof cancelDecisionSchema>

export const recordAppealSchema = z.object({
  appealDate: z.string().regex(DATE_PATTERN, 'تاريخ الاعتراض مطلوب.'),
  appellantDepartmentId: z.string().min(1, 'القسم المعترض مطلوب.'),
  referenceNumber: z.string().trim().max(100, 'الرقم المرجعي لا يتجاوز 100 حرف.'),
  notes: z.string().trim().max(2000, 'الملاحظات لا تتجاوز 2000 حرف.'),
})

export type RecordAppealFormValues = z.infer<typeof recordAppealSchema>

export const recordAppealOutcomeSchema = z.object({
  outcomeId: z.string().min(1, 'اختر النتيجة.'),
  outcomeDate: z.string().regex(DATE_PATTERN, 'تاريخ النتيجة مطلوب.'),
})

export type RecordAppealOutcomeFormValues = z.infer<typeof recordAppealOutcomeSchema>

export const attachDecisionFileSchema = z.object({
  documentFileId: z.string().min(1, 'اختر نسخة القرار.'),
})

export type AttachDecisionFileFormValues = z.infer<typeof attachDecisionFileSchema>
