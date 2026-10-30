import { DOCUMENT_NUMBER_STATUS } from '@/shared/config'
import { Badge, type BadgeVariant } from '@/shared/ui'

const statusMeta: Record<number, { label: string; variant: BadgeVariant }> = {
  [DOCUMENT_NUMBER_STATUS.Active]: { label: 'نشط', variant: 'success' },
  [DOCUMENT_NUMBER_STATUS.Cancelled]: { label: 'ملغى', variant: 'danger' },
  [DOCUMENT_NUMBER_STATUS.Replaced]: { label: 'مُستبدل', variant: 'warning' },
}

interface NumberStatusBadgeProps {
  /** Backend `DocumentNumberStatus` id (1 active, 2 cancelled, 3 replaced). */
  statusId: number
  className?: string
}

export function NumberStatusBadge({ statusId, className }: NumberStatusBadgeProps) {
  const meta = statusMeta[statusId]

  return (
    <Badge variant={meta?.variant ?? 'neutral'} className={className}>
      {meta?.label ?? `حالة #${statusId}`}
    </Badge>
  )
}
