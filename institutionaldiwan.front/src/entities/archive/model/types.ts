import type { DocumentResponse } from '@/entities/document'
import type { PagedQueryParams } from '@/shared/types'

/** One row of `GET /api/archive/documents`: the document plus its current archive stamp. */
export interface ArchivedDocumentResponse {
  document: DocumentResponse
  archivedAt: string
  archivedByUserId: string
  archiveLocationId: number | null
}

/** Sort keys the archive search accepts. */
export type ArchiveSortKey = 'archivedAt' | 'title' | 'officialNumber'

/** Query string of `GET /api/archive/documents`; dates are UTC ISO instants. */
export interface ArchiveSearchParams extends PagedQueryParams {
  sortBy?: ArchiveSortKey
  owningDepartmentId?: string
  documentTypeId?: number
  archiveLocationId?: number
  archivedFromUtc?: string
  archivedToUtc?: string
}

/** One archive transition from `GET /api/documents/{id}/archive-events`. */
export interface DocumentArchiveEventResponse {
  archiveEventId: string
  documentId: string
  eventTypeId: number
  previousStatusId: number
  newStatusId: number
  actorUserId: string
  reason: string | null
  archiveLocationId: number | null
  occurredAt: string
}

/** One personal saved search from `GET /api/saved-searches`. */
export interface SavedSearchResponse {
  savedSearchId: string
  name: string
  criteriaJson: string
  isDefault: boolean
  createdAt: string
  updatedAt: string | null
}
