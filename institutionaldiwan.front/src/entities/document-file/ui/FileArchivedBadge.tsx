import type { LabelKey } from '@/shared/config'
import { useLabel } from '@/shared/lib'
import { Badge, type BadgeVariant } from '@/shared/ui'

interface FileArchivedBadgeProps {
  isArchived: boolean
  className?: string
}

const meta: Record<'active' | 'archived', { key: LabelKey; variant: BadgeVariant }> = {
  active: { key: 'attachment.state.active', variant: 'success' },
  archived: { key: 'attachment.state.archived', variant: 'neutral' },
}

/**
 * Archive state of a document file. Archiving stops new versions but never hides the file or its
 * downloads, so both states stay visible rather than filtering the row out.
 */
export function FileArchivedBadge({ isArchived, className }: FileArchivedBadgeProps) {
  const { key, variant } = meta[isArchived ? 'archived' : 'active']
  const label = useLabel(key)

  return (
    <Badge variant={variant} className={className}>
      {label}
    </Badge>
  )
}
