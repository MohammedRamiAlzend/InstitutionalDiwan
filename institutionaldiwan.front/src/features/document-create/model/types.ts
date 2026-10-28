/** `POST /api/documents` — registers the document with a manual official number. */
export interface CreateDocumentRequest {
  title: string
  documentTypeId: number
  directionId: number
  officialNumberText: string
  numberingYear: number
  subjectSummary?: string | null
  confidentialityLevelId?: number | null
  priorityLevelId?: number | null
}

/** `POST /api/documents/drafts` — creates a DRAFT without consuming a number. */
export interface CreateDraftDocumentRequest {
  title: string
  documentTypeId: number
  directionId: number
  subjectSummary?: string | null
  confidentialityLevelId?: number | null
  priorityLevelId?: number | null
  documentDate?: string | null
  externalReferenceNumber?: string | null
  externalDocumentDate?: string | null
}
