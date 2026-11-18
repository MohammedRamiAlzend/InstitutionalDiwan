import { z } from 'zod'
import { t } from '@/shared/lib'

const DATE_PATTERN = /^\d{4}-\d{2}-\d{2}$/

export const issueDecisionSchema = z.object({
  decisionTypeId: z.string().min(1, t('decisions.issue.validation.type-required')),
  decisionNumber: z.string().trim().min(1, t('decisions.issue.validation.number-required')).max(100, t('decisions.issue.validation.number-max')),
  decisionDate: z.string().regex(DATE_PATTERN, t('decisions.issue.validation.date-required')),
  summary: z.string().trim().max(2000, t('decisions.issue.validation.summary-max')),
  documentFileId: z.string(),
  joinTargetDocumentId: z.string(),
})

export type IssueDecisionFormValues = z.infer<typeof issueDecisionSchema>

export const cancelDecisionSchema = z.object({
  reason: z.string().trim().min(1, t('decisions.cancel.validation.reason-required')).max(1000, t('decisions.cancel.validation.reason-max')),
})

export type CancelDecisionFormValues = z.infer<typeof cancelDecisionSchema>

export const recordAppealSchema = z.object({
  appealDate: z.string().regex(DATE_PATTERN, t('decisions.appeal.validation.date-required')),
  appellantDepartmentId: z.string().min(1, t('decisions.appeal.validation.department-required')),
  referenceNumber: z.string().trim().max(100, t('decisions.appeal.validation.reference-max')),
  notes: z.string().trim().max(2000, t('decisions.appeal.validation.notes-max')),
})

export type RecordAppealFormValues = z.infer<typeof recordAppealSchema>

export const recordAppealOutcomeSchema = z.object({
  outcomeId: z.string().min(1, t('decisions.outcome.validation.outcome-required')),
  outcomeDate: z.string().regex(DATE_PATTERN, t('decisions.outcome.validation.date-required')),
})

export type RecordAppealOutcomeFormValues = z.infer<typeof recordAppealOutcomeSchema>

export const attachDecisionFileSchema = z.object({
  documentFileId: z.string().min(1, t('decisions.attach.validation.file-required')),
})

export type AttachDecisionFileFormValues = z.infer<typeof attachDecisionFileSchema>
