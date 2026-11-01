import { Badge, type BadgeVariant } from '@/shared/ui'

interface FileArchivedBadgeProps {
  isArchived: boolean
  className?: string
}

const meta: Record<'active' | 'archived', { label: string; variant: BadgeVariant }> = {
  active: { label: 'نشط', variant: 'success' },
  archived: { label: 'مؤرشف', variant: 'neutral' },
}

/**
 * Archive state of a document file. Archiving stops new versions but never hides the file or its
 * downloads, so both states stay visible rather than filtering the row out.
 */
export function FileArchivedBadge({ isArchived, className }: FileArchivedBadgeProps) {
  const { label, variant } = meta[isArchived ? 'archived' : 'active']

  return (
    <Badge variant={variant} className={className}>
      {label}
    </Badge>
  )
}
