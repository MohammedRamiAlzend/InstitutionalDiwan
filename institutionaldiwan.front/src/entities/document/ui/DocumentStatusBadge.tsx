import { Badge, type BadgeVariant } from '@/shared/ui'

const statusMeta: Record<number, { label: string; variant: BadgeVariant }> = {
  1: { label: 'مسودة', variant: 'outline' },
  2: { label: 'مسجل', variant: 'success' },
  3: { label: 'قيد التداول', variant: 'accent' },
  4: { label: 'مؤرشف', variant: 'neutral' },
  5: { label: 'ملغي', variant: 'danger' },
}

interface DocumentStatusBadgeProps {
  /** Backend `DocumentStatusType` id (1–5). */
  statusId: number
  className?: string
}

export function DocumentStatusBadge({ statusId, className }: DocumentStatusBadgeProps) {
  const meta = statusMeta[statusId]

  return (
    <Badge variant={meta?.variant ?? 'neutral'} className={className}>
      {meta?.label ?? `حالة #${statusId}`}
    </Badge>
  )
}
