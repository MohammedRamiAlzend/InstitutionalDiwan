import { NOTIFICATION_PRIORITY, LOOKUP_SLUGS, LookupLabel } from '@/entities/lookup'
import { Badge, type BadgeVariant } from '@/shared/ui'

const VARIANTS: Record<number, BadgeVariant> = {
  [NOTIFICATION_PRIORITY.High]: 'warning',
  [NOTIFICATION_PRIORITY.Urgent]: 'danger',
}

/** Shown only for high and urgent notifications; normal ones carry no badge. */
export function NotificationPriorityBadge({ priorityId }: { priorityId: number }) {
  if (priorityId === NOTIFICATION_PRIORITY.Normal) return null

  return (
    <Badge variant={VARIANTS[priorityId] ?? 'outline'}>
      <LookupLabel lookup={LOOKUP_SLUGS.NotificationPriorities} id={priorityId} className="text-xs" />
    </Badge>
  )
}
