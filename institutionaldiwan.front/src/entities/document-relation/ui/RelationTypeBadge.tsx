import { DOCUMENT_RELATION_TYPE, LOOKUP_SLUGS, LookupLabel } from '@/entities/lookup'
import { Badge, type BadgeVariant } from '@/shared/ui'

const VARIANTS: Record<number, BadgeVariant> = {
  [DOCUMENT_RELATION_TYPE.ReplyTo]: 'success',
  [DOCUMENT_RELATION_TYPE.Related]: 'outline',
  [DOCUMENT_RELATION_TYPE.Supersedes]: 'warning',
  [DOCUMENT_RELATION_TYPE.DuplicateOf]: 'neutral',
  [DOCUMENT_RELATION_TYPE.JoinedInto]: 'accent',
}

/** Relation type (reply to, related, supersedes, duplicate of) with its Arabic label. */
export function RelationTypeBadge({ relationTypeId }: { relationTypeId: number }) {
  return (
    <Badge variant={VARIANTS[relationTypeId] ?? 'outline'}>
      <LookupLabel
        lookup={LOOKUP_SLUGS.DocumentRelationTypes}
        id={relationTypeId}
        className="text-xs"
      />
    </Badge>
  )
}
