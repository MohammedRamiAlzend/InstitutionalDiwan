import { useLabel } from '@/shared/lib'
import { Badge, type BadgeVariant } from '@/shared/ui'
import { SYSTEM_ERROR_STATUS_LABEL_KEYS } from '../model/labels'
import { SYSTEM_ERROR_STATUS, type SystemErrorStatus } from '../model/types'

const VARIANTS: Record<SystemErrorStatus, BadgeVariant> = {
  [SYSTEM_ERROR_STATUS.New]: 'danger',
  [SYSTEM_ERROR_STATUS.InProgress]: 'warning',
  [SYSTEM_ERROR_STATUS.Resolved]: 'success',
}

export function SystemErrorStatusBadge({ status }: { status: SystemErrorStatus }) {
  const label = useLabel(SYSTEM_ERROR_STATUS_LABEL_KEYS[status])
  return <Badge variant={VARIANTS[status]}>{label}</Badge>
}
