import type { PagedQueryParams } from '@/shared/types'

/** `DocumentResponse` — the flat document row returned by list and get endpoints. */
export interface DocumentResponse {
  documentId: string
  title: string
  subjectSummary: string | null
  documentTypeId: number
  directionId: number
  confidentialityLevelId: number
  priorityLevelId: number
  statusId: number
  owningDepartmentId: string
  createdByUserId: string
  documentDate: string
  createdAt: string
  officialNumberText: string | null
  registrationDate: string | null
}

/** One party attached to a document. */
export interface DocumentParticipantResponse {
  participantId: string
  roleId: number
  targetTypeId: number
  internalDepartmentId: string | null
  externalOrganizationId: string | null
  personName: string | null
  isPrimary: boolean
  notes: string | null
  createdAt: string
}

/** One recorded status transition. */
export interface DocumentStatusHistoryResponse {
  historyId: string
  fromStatusId: number | null
  toStatusId: number
  changedByUserId: string
  reason: string | null
  changedAt: string
}

/** `GET /api/documents/{documentId}/details`. */
export interface DocumentDetailResponse {
  document: DocumentResponse
  participants: DocumentParticipantResponse[]
  statusHistory: DocumentStatusHistoryResponse[]
}

/** One audit-timeline entry; `eventKind` is `STATUS`, `PARTICIPANT`, or `NUMBER`. */
export interface DocumentTimelineEntryResponse {
  eventKind: 'STATUS' | 'PARTICIPANT' | 'NUMBER'
  occurredAt: string
  actorUserId: string
  description: string | null
  participantId: string | null
  fromStatusId: number | null
  toStatusId: number | null
  documentNumberId: string | null
  numberEventTypeId: number | null
}

/** One per-document access grant (Module 02 access control on Module 03 documents). */
export interface DocumentAccessGrantResponse {
  grantId: string
  documentId: string
  userId: string
  accessLevelId: number
  grantedByUserId: string
  reason: string | null
  grantedAt: string
  expiresAt: string | null
  isRevoked: boolean
  revokedAt: string | null
  revokedByUserId: string | null
}

/** URL-driven registry filters layered over the shared paging params. */
export interface DocumentListParams extends PagedQueryParams {
  statusId?: number
  owningDepartmentId?: string
  directionId?: number
  documentTypeId?: number
}
