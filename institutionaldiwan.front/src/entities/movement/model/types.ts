/** One hop of a movement: the department pair and what happened to the step. */
export interface MovementStepResponse {
  stepId: string
  stepNo: number
  fromDepartmentId: string
  toDepartmentId: string
  assignedToUserId: string | null
  stepStatusId: number
  instructions: string | null
  outcomeNotes: string | null
  sentAt: string | null
  receivedAt: string | null
  receivedByUserId: string | null
  registryNumberId: string | null
  completedAt: string | null
  dueDate: string | null
}

/** One entry of a movement's audit trail. */
export interface MovementEventResponse {
  eventId: string
  stepNo: number
  eventTypeId: number
  actorUserId: string
  notes: string | null
  occurredAt: string
}

/** `GET /api/movements/{id}` and every command that returns the full movement. */
export interface MovementResponse {
  movementId: string
  documentId: string
  movementTypeId: number
  sourceDepartmentId: string
  destinationDepartmentId: string
  assignedToUserId: string | null
  statusId: number
  currentStepNo: number
  instructions: string | null
  dueDate: string | null
  createdByUserId: string
  createdAt: string
  closedAt: string | null
  steps: MovementStepResponse[]
  events: MovementEventResponse[]
}

/** One row of a department inbox or outbox (`GET /api/movements/inbox|outbox`). */
export interface MovementQueueItemResponse {
  movementId: string
  documentId: string
  documentTitle: string
  officialNumberText: string | null
  movementTypeId: number
  movementStatusId: number
  stepNo: number
  stepStatusId: number
  fromDepartmentId: string
  toDepartmentId: string
  assignedToUserId: string | null
  instructions: string | null
  dueDate: string | null
  createdAt: string
  sentAt: string | null
  receivedAt: string | null
  completedAt: string | null
}

/** `POST /api/movements/{id}/receive` — the register line that authorised the receipt. */
export interface MovementReceiptResponse {
  movementId: string
  stepNo: number
  departmentId: string
  registryNumberId: string
  registryNumberText: string
  registryDate: string | null
  receivedAt: string
}

export type MovementQueueKind = 'inbox' | 'outbox'

/** Workflow actions exposed by the movement routes (opening lives in `movement-routing`). */
export type MovementAction =
  | 'send'
  | 'receive'
  | 'complete'
  | 'reject'
  | 'return'
  | 'redirect'
  | 'close'
  | 'cancel'
