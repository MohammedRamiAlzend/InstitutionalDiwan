import { relationPhrase } from '../model/direction'
import type { RelationDirection } from '../model/types'

/** The link read from the viewed document, e.g. «هذه الوثيقة ردّ على». */
export function RelationDirectionLabel({
  relationTypeId,
  direction,
}: {
  relationTypeId: number
  direction: RelationDirection
}) {
  return <span className="text-sm">{relationPhrase(relationTypeId, direction)}</span>
}
