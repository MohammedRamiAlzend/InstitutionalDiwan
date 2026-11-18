import { type LabelKey } from '@/shared/config'
import { useLabel } from '@/shared/lib'
import { Badge, type BadgeVariant } from '@/shared/ui'

const statusMeta: Record<number, { key: LabelKey; variant: BadgeVariant }> = {
  1: { key: 'docdetails.status.draft', variant: 'outline' },
  2: { key: 'docdetails.status.registered', variant: 'success' },
  3: { key: 'docdetails.status.in-movement', variant: 'accent' },
  4: { key: 'docdetails.status.archived', variant: 'neutral' },
  5: { key: 'docdetails.status.cancelled', variant: 'danger' },
}

interface DocumentStatusBadgeProps {
  /** Backend `DocumentStatusType` id (1–5). */
  statusId: number
  className?: string
}

export function DocumentStatusBadge({ statusId, className }: DocumentStatusBadgeProps) {
  const meta = statusMeta[statusId]
  const label = useLabel(meta?.key ?? 'docdetails.status.unknown', { id: statusId })

  return (
    <Badge variant={meta?.variant ?? 'neutral'} className={className}>
      {label}
    </Badge>
  )
}