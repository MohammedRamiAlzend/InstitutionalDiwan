import { LOOKUP_SLUGS, LookupLabel } from '@/entities/lookup'
import { MOVEMENT_STATUS } from '@/shared/config'
import { Badge, type BadgeVariant } from '@/shared/ui'

const statusVariants: Record<number, BadgeVariant> = {
  [MOVEMENT_STATUS.Draft]: 'neutral',
  [MOVEMENT_STATUS.Sent]: 'accent',
  [MOVEMENT_STATUS.Received]: 'warning',
  [MOVEMENT_STATUS.InProgress]: 'accent',
  [MOVEMENT_STATUS.Completed]: 'success',
  [MOVEMENT_STATUS.Rejected]: 'danger',
  [MOVEMENT_STATUS.Returned]: 'warning',
  [MOVEMENT_STATUS.Redirected]: 'accent',
  [MOVEMENT_STATUS.Closed]: 'success',
  [MOVEMENT_STATUS.Canceled]: 'danger',
}

/** Movement lifecycle status with its Arabic label from the seeded catalog. */
export function MovementStatusBadge({ statusId }: { statusId: number }) {
  return (
    <Badge variant={statusVariants[statusId] ?? 'neutral'}>
      <LookupLabel
        lookup={LOOKUP_SLUGS.MovementStatuses}
        id={statusId}
        className="text-xs"
      />
    </Badge>
  )
}
