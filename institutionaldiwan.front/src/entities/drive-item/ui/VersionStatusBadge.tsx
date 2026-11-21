import { DRIVE_VERSION_STATUS, type LabelKey } from '@/shared/config'
import { useLabel } from '@/shared/lib'
import { Badge, type BadgeVariant } from '@/shared/ui'

const meta: Record<number, { key: LabelKey; variant: BadgeVariant }> = {
  [DRIVE_VERSION_STATUS.Pending]: { key: 'drive.version-status.pending', variant: 'warning' },
  [DRIVE_VERSION_STATUS.Committed]: { key: 'drive.version-status.committed', variant: 'success' },
  [DRIVE_VERSION_STATUS.Abandoned]: { key: 'drive.version-status.abandoned', variant: 'outline' },
}

/** Lifecycle state of a file version: pending upload / committed / abandoned. */
export function VersionStatusBadge({ statusId, className }: { statusId: number; className?: string }) {
  const { key, variant } = meta[statusId] ?? meta[DRIVE_VERSION_STATUS.Pending]
  const label = useLabel(key)

  return (
    <Badge variant={variant} className={className}>
      {label}
    </Badge>
  )
}