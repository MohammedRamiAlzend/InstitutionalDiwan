/** `PUT /api/documents/{documentId}` — metadata edit accepted only while the document is a DRAFT. */
export interface UpdateDraftDocumentRequest {
  title: string
  subjectSummary?: string | null
  confidentialityLevelId?: number | null
  priorityLevelId?: number | null
}

/** `POST /api/documents/{documentId}/register` — omit the year to use the document/current year. */
export interface RegisterDraftDocumentRequest {
  numberingYear?: number | null
}

/** `POST /api/documents/{documentId}/participants` — exactly one target must be supplied. */
export interface AddDocumentParticipantRequest {
  roleId: number
  externalOrganizationId?: string | null
  internalDepartmentId?: string | null
  personName?: string | null
  isPrimary?: boolean
  notes?: string | null
}

/** `POST /api/documents/{documentId}/cancel` — the reason is required by the server. */
export interface CancelDocumentRequest {
  reason: string
}
