import { APPEAL_OUTCOME, DECISION_EFFECT, DECISION_STATUS } from './lookupIds'

const EFFECT_LABELS: Record<number, string> = {
  [DECISION_EFFECT.Refer]: 'إحالة',
  [DECISION_EFFECT.SendToArchive]: 'إرسال للحفظ',
  [DECISION_EFFECT.Join]: 'توحيد',
  [DECISION_EFFECT.None]: 'بلا أثر',
}

const STATUS_LABELS: Record<number, string> = {
  [DECISION_STATUS.Issued]: 'صادر',
  [DECISION_STATUS.Appealed]: 'قيد الاعتراض',
  [DECISION_STATUS.Final]: 'نهائي',
  [DECISION_STATUS.Revoked]: 'مفسوخ',
  [DECISION_STATUS.Canceled]: 'أُلغي الإدخال',
}

export function decisionEffectLabel(effectId: number): string {
  return EFFECT_LABELS[effectId] ?? `أثر ${effectId}`
}

export function decisionStatusLabel(statusId: number): string {
  return STATUS_LABELS[statusId] ?? `حالة ${statusId}`
}

const APPEAL_OUTCOME_LABELS: Record<number, string> = {
  [APPEAL_OUTCOME.Pending]: 'معلّق',
  [APPEAL_OUTCOME.Accepted]: 'مقبول',
  [APPEAL_OUTCOME.Rejected]: 'مردود',
}

export function appealOutcomeLabel(outcomeId: number): string {
  return APPEAL_OUTCOME_LABELS[outcomeId] ?? `نتيجة ${outcomeId}`
}

/** Issued, appealed, or final. A revoked or canceled decision is no longer in force. */
export function isDecisionInForce(statusId: number): boolean {
  return (
    statusId === DECISION_STATUS.Issued ||
    statusId === DECISION_STATUS.Appealed ||
    statusId === DECISION_STATUS.Final
  )
}
