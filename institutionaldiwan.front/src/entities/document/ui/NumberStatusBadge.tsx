import { Badge, type BadgeVariant } from '@/shared/ui'

const statusMeta: Record<number, { label: string; variant: BadgeVariant }> = {
  1: { label: 'نشط', variant: 'success' },
  2: { label: 'ملغى', variant: 'danger' },
  3: { label: 'مُستبدل', variant: 'warning' },
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
