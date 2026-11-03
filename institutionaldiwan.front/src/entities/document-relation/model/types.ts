import type { PagedResult } from '@/shared/types'

/** How a link reads from the document that is being viewed. */
export type RelationDirection = 'Outgoing' | 'Incoming' | 'Symmetric'

/** A relation type with the flags the client needs to draw and explain a link. */
export interface RelationTypeResponse {
  relationTypeId: number
  desc: string
  descAr: string
  isSymmetric: boolean
  preventCycles: boolean
}

/** The few document fields shown next to a link, never the document's content. */
export interface RelatedDocumentSummary {
  documentId: string
  title: string
  officialNumberText: string | null
  statusId: number
  isArchived: boolean
}

/** One link of a document, read from the document it was requested for. */
export interface DocumentRelationResponse {
  relationId: string
  sourceDocumentId: string
  targetDocumentId: string
  direction: RelationDirection
  type: RelationTypeResponse
  relatedDocument: RelatedDocumentSummary
  notes: string | null
  createdByUserId: string
  createdAt: string
}

/** A document's links; `hiddenRelationCount` counts links to documents the caller may not view. */
export interface DocumentRelationsResponse {
  items: DocumentRelationResponse[]
  hiddenRelationCount: number
}

export interface RelationGraphNode {
  document: RelatedDocumentSummary
  /** Distance from the document the graph is rooted at. */
  depth: number
}

export interface RelationGraphEdge {
  relationId: string
  sourceDocumentId: string
  targetDocumentId: string
  relationTypeId: number
}

export interface RelationGraphResponse {
  rootDocumentId: string
  nodes: RelationGraphNode[]
  edges: RelationGraphEdge[]
  types: RelationTypeResponse[]
  hiddenRelationCount: number
  /** True when the 100-node limit stopped the walk early. */
  truncated: boolean
}

/** One entry of the permanent relation history, which also covers removed links. */
export interface DocumentRelationEventResponse {
  relationEventId: string
  relationId: string
  sourceDocumentId: string
  targetDocumentId: string
  relationTypeId: number
  actionTypeId: number
  actorUserId: string
  reason: string | null
  occurredAt: string
}

export type RelationEventsPage = PagedResult<DocumentRelationEventResponse>

export const MIN_GRAPH_DEPTH = 1
export const MAX_GRAPH_DEPTH = 3
export const DEFAULT_GRAPH_DEPTH = 2
