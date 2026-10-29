import { LOOKUP_SLUGS, LookupLabel } from '@/entities/lookup'
import { Badge } from '@/shared/ui'

/** Movement type (referral, transfer, copy, return) with its Arabic catalog label. */
export function MovementTypeBadge({ movementTypeId }: { movementTypeId: number }) {
  return (
    <Badge variant="outline">
      <LookupLabel
        lookup={LOOKUP_SLUGS.MovementTypes}
        id={movementTypeId}
        className="text-xs"
      />
    </Badge>
  )
}
