import { DRIVE_ROLE, type LabelKey } from '@/shared/config'
import { useLabel } from '@/shared/lib'
import { Badge, type BadgeVariant } from '@/shared/ui'

const meta: Record<number, { key: LabelKey; variant: BadgeVariant }> = {
  [DRIVE_ROLE.None]: { key: 'drive.role.none', variant: 'outline' },
  [DRIVE_ROLE.Viewer]: { key: 'drive.role.viewer', variant: 'neutral' },
  [DRIVE_ROLE.Editor]: { key: 'drive.role.editor', variant: 'accent' },
  [DRIVE_ROLE.Manager]: { key: 'drive.role.manager', variant: 'success' },
}

/** The effective (or granted) Drive role: none / viewer / editor / manager. */
export function RoleBadge({ roleId, className }: { roleId: number; className?: string }) {
  const { key, variant } = meta[roleId] ?? meta[DRIVE_ROLE.None]
  const label = useLabel(key)

  return (
    <Badge variant={variant} className={className}>
      {label}
    </Badge>
  )
}