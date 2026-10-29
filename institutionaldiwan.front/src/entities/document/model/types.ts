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

/** One event on an official number (issued, cancelled, replaced, department-registered). */
export interface DocumentNumberHistoryEventResponse {
  historyId: string
  eventTypeId: number
  actorUserId: string
  notes: string | null
  occurredAt: string
  effectiveDate: string | null
}

/** One official number issued for a document (`GET /api/documents/{id}/numbers/history`). */
export interface DocumentNumberResponse {
  documentNumberId: string
  documentId: string
  registryId: string
  scopeId: number
  departmentId: string | null
  numberingYear: number
  sequenceValue: number
  officialNumberText: string
  registryDate: string | null
  statusId: number
  issuedByUserId: string
  issuedAt: string
  replacedByNumberId: string | null
  cancellationReason: string | null
  history: DocumentNumberHistoryEventResponse[]
}

/** One dated entry in a department's own register for this document. */
export interface DepartmentRegistrationEventResponse {
  historyId: string
  eventTypeId: number
  effectiveDate: string | null
  occurredAt: string
  actorUserId: string
  notes: string | null
}

/** A department's register line for the document, with its dated registrations. */
export interface DepartmentRegistryNumberResponse {
  documentNumberId: string
  departmentId: string
  registryId: string
  registryNumberText: string
  numberingYear: number
  registryDate: string | null
  statusId: number
  registrations: DepartmentRegistrationEventResponse[]
}

/** Central number plus every department register line (`GET .../numbers/registry-entries`). */
export interface DocumentRegistryNumbersResponse {
  documentId: string
  centralNumberId: string | null
  centralNumberText: string | null
  centralRegistrationDate: string | null
  departmentNumbers: DepartmentRegistryNumberResponse[]
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
