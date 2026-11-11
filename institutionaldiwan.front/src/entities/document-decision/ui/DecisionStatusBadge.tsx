import { DECISION_STATUS, decisionStatusLabel } from '@/shared/config'
import { Badge, type BadgeVariant } from '@/shared/ui'

const VARIANTS: Record<number, BadgeVariant> = {
  [DECISION_STATUS.Issued]: 'accent',
  [DECISION_STATUS.Appealed]: 'warning',
  [DECISION_STATUS.Final]: 'success',
  [DECISION_STATUS.Revoked]: 'neutral',
  [DECISION_STATUS.Canceled]: 'danger',
}

export function DecisionStatusBadge({ statusId }: { statusId: number }) {
  return <Badge variant={VARIANTS[statusId] ?? 'outline'}>{decisionStatusLabel(statusId)}</Badge>
}
