import type { FieldValueRequest } from '@/entities/document-schema'

/** `POST /api/movements` — the source is the caller's primary department. */
export interface CreateMovementRequest {
  documentId: string
  destinationDepartmentId: string
  movementTypeId?: number
  assignedToUserId?: string | null
  instructions?: string | null
  dueDate?: string | null
}

/** Optional notes carried by send/receive/complete/close. */
export interface MovementNotesRequest {
  notes?: string | null
}

/** `POST /api/movements/{id}/receive` — the number is needed only for a department's first arrival in a year. */
export interface ReceiveMovementRequest extends MovementNotesRequest {
  registryDate: string
  registryNumberText?: string | null
  /** Register for a new number; omitted = the best matching active one. */
  registryId?: string | null
  /** Values of the destination department's record form for the document type, when one is published. */
  fieldValues?: FieldValueRequest[]
  /** Write the document under `registryNumberText` although the department has a number that year; the old one becomes replaced and linked. */
  issueNewNumber?: boolean
}

/** Required reason carried by reject/return/cancel. */
export interface MovementReasonRequest {
  reason: string
}

/** `POST /api/movements/{id}/redirect` — opens a new step without closing the movement. */
export interface RedirectMovementRequest {
  newDestinationDepartmentId: string
  assignedToUserId?: string | null
  instructions?: string | null
  dueDate?: string | null
}
