import { LOOKUP_SLUGS, LookupLabel } from '@/entities/lookup'
import { MOVEMENT_STEP_STATUS } from '@/shared/config'
import { Badge, type BadgeVariant } from '@/shared/ui'

const stepVariants: Record<number, BadgeVariant> = {
  [MOVEMENT_STEP_STATUS.Pending]: 'neutral',
  [MOVEMENT_STEP_STATUS.Sent]: 'accent',
  [MOVEMENT_STEP_STATUS.Received]: 'warning',
  [MOVEMENT_STEP_STATUS.Completed]: 'success',
  [MOVEMENT_STEP_STATUS.Rejected]: 'danger',
  [MOVEMENT_STEP_STATUS.Returned]: 'warning',
  [MOVEMENT_STEP_STATUS.Redirected]: 'accent',
}

/** Current-step status with its Arabic label from the seeded catalog. */
export function MovementStepStatusBadge({ stepStatusId }: { stepStatusId: number }) {
  return (
    <Badge variant={stepVariants[stepStatusId] ?? 'neutral'}>
      <LookupLabel
        lookup={LOOKUP_SLUGS.MovementStepStatuses}
        id={stepStatusId}
        className="text-xs"
      />
    </Badge>
  )
}
