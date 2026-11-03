/** Body of `POST /api/documents/{id}/archive`. */
export interface ArchiveDocumentRequest {
  archiveLocationId: number
  reason?: string
}

/** Body of `POST /api/documents/{id}/unarchive`; the reason is mandatory. */
export interface UnarchiveDocumentRequest {
  reason: string
}

/** Body of `POST /api/saved-searches` and `PUT /api/saved-searches/{id}`. */
export interface SaveSearchRequest {
  name: string
  criteriaJson: string
  isDefault: boolean
}
