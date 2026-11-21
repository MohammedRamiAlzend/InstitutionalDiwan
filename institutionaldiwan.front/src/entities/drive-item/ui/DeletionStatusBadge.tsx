import { DRIVE_DELETION_STATUS, type LabelKey } from '@/shared/config'
import { useLabel } from '@/shared/lib'
import { Badge, type BadgeVariant } from '@/shared/ui'

const meta: Record<number, { key: LabelKey; variant: BadgeVariant }> = {
  [DRIVE_DELETION_STATUS.Pending]: { key: 'drive.deletion-status.pending', variant: 'warning' },
  [DRIVE_DELETION_STATUS.Approved]: { key: 'drive.deletion-status.approved', variant: 'success' },
  [DRIVE_DELETION_STATUS.Rejected]: { key: 'drive.deletion-status.rejected', variant: 'danger' },
  [DRIVE_DELETION_STATUS.Cancelled]: { key: 'drive.deletion-status.cancelled', variant: 'outline' },
}

/** Review state of an approval-gated deletion request. */
export function DeletionStatusBadge({ statusId, className }: { statusId: number; className?: string }) {
  const { key, variant } = meta[statusId] ?? meta[DRIVE_DELETION_STATUS.Pending]
  const label = useLabel(key)

  return (
    <Badge variant={variant} className={className}>
      {label}
    </Badge>
  )
}