import type { FieldValueRequest } from '@/entities/document-schema'

/** Optional routing sent together with the new document; the movement is opened and sent at once. */
export interface CreateDocumentMovementRequest {
  destinationDepartmentId: string
  movementTypeId?: number
  assignedToUserId?: string | null
  instructions?: string | null
  dueDate?: string | null
}

/**
 * `POST /api/documents` — registers the document with a system-issued number, writes the
 * department register line (number + date typed by the user) and optionally sends it on.
 */
export interface CreateDocumentRequest {
  title: string
  documentTypeId: number
  departmentRegistryNumberText: string
  departmentRegistryDate: string
  movement?: CreateDocumentMovementRequest | null
  subjectSummary?: string | null
  /** Dynamic field values for the type's published schema; omit when the type has none. */
  fieldValues?: FieldValueRequest[]
}

/** `POST /api/documents/drafts` — creates a DRAFT without consuming a number. */
export interface CreateDraftDocumentRequest {
  title: string
  documentTypeId: number
  subjectSummary?: string | null
  fieldValues?: FieldValueRequest[]
  documentDate?: string | null
  externalReferenceNumber?: string | null
  externalDocumentDate?: string | null
}

/** `POST /api/documents/{documentId}/participants` — optional recipient captured at creation. */
export interface CreateDocumentRecipientRequest {
  roleId: number
  externalOrganizationId?: string | null
  internalDepartmentId?: string | null
  personName?: string | null
  isPrimary?: boolean
  notes?: string | null
}
