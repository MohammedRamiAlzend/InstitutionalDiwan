import { ARCHIVE_EVENT_TYPE, LOOKUP_SLUGS, LookupLabel } from '@/entities/lookup'
import { Badge, type BadgeVariant } from '@/shared/ui'

const VARIANTS: Record<number, BadgeVariant> = {
  [ARCHIVE_EVENT_TYPE.Archived]: 'success',
  [ARCHIVE_EVENT_TYPE.Unarchived]: 'warning',
  [ARCHIVE_EVENT_TYPE.RetentionApplied]: 'outline',
}

/** Archive history event type (archived, unarchived, retention applied) with its Arabic label. */
export function ArchiveEventTypeBadge({ eventTypeId }: { eventTypeId: number }) {
  return (
    <Badge variant={VARIANTS[eventTypeId] ?? 'outline'}>
      <LookupLabel lookup={LOOKUP_SLUGS.ArchiveEventTypes} id={eventTypeId} className="text-xs" />
    </Badge>
  )
}
