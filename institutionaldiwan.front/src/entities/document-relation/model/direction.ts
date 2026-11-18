import { DOCUMENT_RELATION_TYPE, type LabelKey } from '@/shared/config'
import { t } from '@/shared/lib'
import type { RelationDirection } from './types'

/**
 * Reads a link from the viewed document: the server reports only the stored
 * direction, so the inverse phrasing is composed here.
 */
const PHRASE_KEYS: Record<number, Record<RelationDirection, LabelKey>> = {
  [DOCUMENT_RELATION_TYPE.ReplyTo]: {
    Outgoing: 'relation.phrase.reply-outgoing',
    Incoming: 'relation.phrase.reply-incoming',
    Symmetric: 'relation.phrase.linked',
  },
  [DOCUMENT_RELATION_TYPE.Related]: {
    Outgoing: 'relation.phrase.linked',
    Incoming: 'relation.phrase.linked',
    Symmetric: 'relation.phrase.linked',
  },
  [DOCUMENT_RELATION_TYPE.Supersedes]: {
    Outgoing: 'relation.phrase.supersedes-outgoing',
    Incoming: 'relation.phrase.supersedes-incoming',
    Symmetric: 'relation.phrase.linked',
  },
  [DOCUMENT_RELATION_TYPE.DuplicateOf]: {
    Outgoing: 'relation.phrase.duplicate-outgoing',
    Incoming: 'relation.phrase.duplicate-incoming',
    Symmetric: 'relation.phrase.linked',
  },
  [DOCUMENT_RELATION_TYPE.JoinedInto]: {
    Outgoing: 'relation.phrase.joined-outgoing',
    Incoming: 'relation.phrase.joined-incoming',
    Symmetric: 'relation.phrase.linked',
  },
}

const FALLBACK_KEYS: Record<RelationDirection, LabelKey> = {
  Outgoing: 'relation.phrase.fallback-outgoing',
  Incoming: 'relation.phrase.fallback-incoming',
  Symmetric: 'relation.phrase.linked',
}

/** Catalog key for the link phrase as seen from the viewed document. */
export function relationPhraseKey(relationTypeId: number, direction: RelationDirection): LabelKey {
  return PHRASE_KEYS[relationTypeId]?.[direction] ?? FALLBACK_KEYS[direction]
}

/** Arabic phrase describing the link as seen from the viewed document (non-reactive). */
export function relationPhrase(relationTypeId: number, direction: RelationDirection): string {
  return t(relationPhraseKey(relationTypeId, direction))
}

export const RELATION_DIRECTION_LABELS: Record<RelationDirection, LabelKey> = {
  Outgoing: 'relation.direction.outgoing',
  Incoming: 'relation.direction.incoming',
  Symmetric: 'relation.direction.symmetric',
}