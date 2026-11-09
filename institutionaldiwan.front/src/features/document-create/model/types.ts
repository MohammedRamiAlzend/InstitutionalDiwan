import type { FieldValueRequest } from '@/entities/document-schema'

/** A person named on the new document: an existing `personId`, or the `fullName` of a new one. */
export interface CreateDocumentPersonRequest {
  personId?: string
  fullName?: string
}

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
  /** Register of the creator's department; omitted = the best matching active one. */
  departmentRegistryId?: string | null
  movement?: CreateDocumentMovementRequest | null
  subjectSummary?: string | null
  /** Dynamic field values for the type's published schema; omit when the type has none. */
  fieldValues?: FieldValueRequest[]
  persons?: CreateDocumentPersonRequest[]
}

/** `POST /api/documents/drafts` — creates a DRAFT without consuming a number. */
export interface CreateDraftDocumentRequest {
  title: string
  documentTypeId: number
  subjectSummary?: string | null
  fieldValues?: FieldValueRequest[]
  persons?: CreateDocumentPersonRequest[]
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
