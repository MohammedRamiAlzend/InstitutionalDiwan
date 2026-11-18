import { useLabel } from '@/shared/lib'
import { relationPhraseKey } from '../model/direction'
import type { RelationDirection } from '../model/types'

/** The link read from the viewed document, e.g. «هذه الوثيقة ردّ على». */
export function RelationDirectionLabel({
  relationTypeId,
  direction,
}: {
  relationTypeId: number
  direction: RelationDirection
}) {
  const phrase = useLabel(relationPhraseKey(relationTypeId, direction))
  return <span className="text-sm">{phrase}</span>
}