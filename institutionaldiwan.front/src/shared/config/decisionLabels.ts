import { t } from '@/shared/lib'
import type { LabelKey } from './labels'
import { APPEAL_OUTCOME, DECISION_EFFECT, DECISION_STATUS } from './lookupIds'

const EFFECT_LABELS: Record<number, LabelKey> = {
  [DECISION_EFFECT.Refer]: 'decisions.effect.refer',
  [DECISION_EFFECT.SendToArchive]: 'decisions.effect.send-to-archive',
  [DECISION_EFFECT.Join]: 'decisions.effect.join',
  [DECISION_EFFECT.None]: 'decisions.effect.none',
}

const STATUS_LABELS: Record<number, LabelKey> = {
  [DECISION_STATUS.Issued]: 'decisions.status.issued',
  [DECISION_STATUS.Appealed]: 'decisions.status.appealed',
  [DECISION_STATUS.Final]: 'decisions.status.final',
  [DECISION_STATUS.Revoked]: 'decisions.status.revoked',
  [DECISION_STATUS.Canceled]: 'decisions.status.canceled',
}

const APPEAL_OUTCOME_LABELS: Record<number, LabelKey> = {
  [APPEAL_OUTCOME.Pending]: 'decisions.appeal-outcome.pending',
  [APPEAL_OUTCOME.Accepted]: 'decisions.appeal-outcome.accepted',
  [APPEAL_OUTCOME.Rejected]: 'decisions.appeal-outcome.rejected',
}

export function decisionEffectLabel(effectId: number): string {
  return t(EFFECT_LABELS[effectId] ?? 'decisions.effect.fallback', { id: effectId })
}

export function decisionStatusLabel(statusId: number): string {
  return t(STATUS_LABELS[statusId] ?? 'decisions.status.fallback', { id: statusId })
}

export function appealOutcomeLabel(outcomeId: number): string {
  return t(APPEAL_OUTCOME_LABELS[outcomeId] ?? 'decisions.appeal-outcome.fallback', { id: outcomeId })
}

/** Issued, appealed, or final. A revoked or canceled decision is no longer in force. */
export function isDecisionInForce(statusId: number): boolean {
  return (
    statusId === DECISION_STATUS.Issued ||
    statusId === DECISION_STATUS.Appealed ||
    statusId === DECISION_STATUS.Final
  )
}