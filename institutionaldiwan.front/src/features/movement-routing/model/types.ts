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
