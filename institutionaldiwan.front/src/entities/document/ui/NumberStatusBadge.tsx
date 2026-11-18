import { DOCUMENT_NUMBER_STATUS, type LabelKey } from '@/shared/config'
import { useLabel } from '@/shared/lib'
import { Badge, type BadgeVariant } from '@/shared/ui'

const statusMeta: Record<number, { key: LabelKey; variant: BadgeVariant }> = {
  [DOCUMENT_NUMBER_STATUS.Active]: { key: 'documents.number-status.active', variant: 'success' },
  [DOCUMENT_NUMBER_STATUS.Cancelled]: { key: 'documents.number-status.cancelled', variant: 'danger' },
  [DOCUMENT_NUMBER_STATUS.Replaced]: { key: 'documents.number-status.replaced', variant: 'warning' },
}

interface NumberStatusBadgeProps {
  /** Backend `DocumentNumberStatus` id (1 active, 2 cancelled, 3 replaced). */
  statusId: number
  className?: string
}

export function NumberStatusBadge({ statusId, className }: NumberStatusBadgeProps) {
  const meta = statusMeta[statusId]
  const label = useLabel(meta?.key ?? 'documents.number-status.unknown', { id: statusId })

  return (
    <Badge variant={meta?.variant ?? 'neutral'} className={className}>
      {label}
    </Badge>
  )
}