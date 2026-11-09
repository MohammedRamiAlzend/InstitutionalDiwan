
/**
 * `POST /api/documents/{documentId}/register` — the system issues the central number; the typed
 * register number and date go into the caller's department register, optionally with a movement.
 */
export interface RegisterDraftDocumentRequest {
  departmentRegistryNumberText: string
  departmentRegistryDate: string
  /** Register of the caller's department; omitted = the best matching active one. */
  departmentRegistryId?: string | null
  movement?: {
    destinationDepartmentId: string
    movementTypeId: number
    instructions?: string | null
  } | null
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
