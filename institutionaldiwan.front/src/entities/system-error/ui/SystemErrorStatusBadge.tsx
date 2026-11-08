import { Badge, type BadgeVariant } from '@/shared/ui'
import { SYSTEM_ERROR_STATUS_LABELS } from '../model/labels'
import { SYSTEM_ERROR_STATUS, type SystemErrorStatus } from '../model/types'

const VARIANTS: Record<SystemErrorStatus, BadgeVariant> = {
  [SYSTEM_ERROR_STATUS.New]: 'danger',
  [SYSTEM_ERROR_STATUS.InProgress]: 'warning',
  [SYSTEM_ERROR_STATUS.Resolved]: 'success',
}

export function SystemErrorStatusBadge({ status }: { status: SystemErrorStatus }) {
  return <Badge variant={VARIANTS[status]}>{SYSTEM_ERROR_STATUS_LABELS[status]}</Badge>
}
