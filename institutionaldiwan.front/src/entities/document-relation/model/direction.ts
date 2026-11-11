import { DOCUMENT_RELATION_TYPE } from '@/shared/config'
import type { RelationDirection } from './types'

/**
 * Reads a link from the viewed document: the server reports only the stored
 * direction, so the inverse phrasing is composed here.
 */
const PHRASES: Record<number, Record<RelationDirection, string>> = {
  [DOCUMENT_RELATION_TYPE.ReplyTo]: {
    Outgoing: 'هذه الوثيقة ردّ على',
    Incoming: 'تلقّت هذه الوثيقة ردًّا من',
    Symmetric: 'مرتبطة بـ',
  },
  [DOCUMENT_RELATION_TYPE.Related]: {
    Outgoing: 'مرتبطة بـ',
    Incoming: 'مرتبطة بـ',
    Symmetric: 'مرتبطة بـ',
  },
  [DOCUMENT_RELATION_TYPE.Supersedes]: {
    Outgoing: 'هذه الوثيقة تلغي',
    Incoming: 'أُلغيت هذه الوثيقة بـ',
    Symmetric: 'مرتبطة بـ',
  },
  [DOCUMENT_RELATION_TYPE.DuplicateOf]: {
    Outgoing: 'هذه الوثيقة نسخة عن',
    Incoming: 'لهذه الوثيقة نسخة هي',
    Symmetric: 'مرتبطة بـ',
  },
  [DOCUMENT_RELATION_TYPE.JoinedInto]: {
    Outgoing: 'هذه الوثيقة موحَّدة مع',
    Incoming: 'وُحِّد في هذه الوثيقة',
    Symmetric: 'مرتبطة بـ',
  },
}

const FALLBACK: Record<RelationDirection, string> = {
  Outgoing: 'صادر إلى',
  Incoming: 'وارد من',
  Symmetric: 'مرتبطة بـ',
}

/** Arabic phrase describing the link as seen from the viewed document. */
export function relationPhrase(relationTypeId: number, direction: RelationDirection): string {
  return PHRASES[relationTypeId]?.[direction] ?? FALLBACK[direction]
}

export const RELATION_DIRECTION_LABELS: Record<RelationDirection, string> = {
  Outgoing: 'صادر',
  Incoming: 'وارد',
  Symmetric: 'متبادل',
}
