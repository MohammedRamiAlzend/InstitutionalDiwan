/**
 * Deterministic lookup ids — must match the API's seeded lookup entities
 * (`UserScopeType`, `AssignmentType` in IdentityLookups.cs and `DepartmentType`).
 */
export const USER_SCOPE_TYPE = {
  Department: 1,
  DepartmentTree: 2,
  Institution: 3,
  System: 4,
} as const

export const ASSIGNMENT_TYPE = {
  Primary: 1,
  Additional: 2,
} as const

export const DEPARTMENT_TYPE = {
  Directorate: 1,
  Department: 2,
  Section: 3,
  Unit: 4,
  CentralDiwan: 5,
} as const

export const NUMBERING_MODE = {
  Automatic: 1,
  Manual: 2,
} as const

export const NUMBERING_RESET_POLICY = {
  Yearly: 1,
  Monthly: 2,
  Never: 3,
} as const

export const DOCUMENT_DIRECTION = {
  Internal: 1,
  Incoming: 2,
  Outgoing: 3,
  All: 4,
} as const

export const DOCUMENT_TYPE = {
  IncomingLetter: 1,
  OutgoingLetter: 2,
  InternalMemo: 3,
  Circular: 4,
  Decision: 5,
} as const

export const DOCUMENT_STATUS = {
  Draft: 1,
  Registered: 2,
  InMovement: 3,
  Archived: 4,
  Canceled: 5,
} as const

export const DOCUMENT_CONFIDENTIALITY = {
  Normal: 1,
  Confidential: 2,
  Secret: 3,
  TopSecret: 4,
} as const

export const DOCUMENT_PRIORITY = {
  Normal: 1,
  Urgent: 2,
  Immediate: 3,
} as const

export const PARTICIPANT_ROLE = {
  Sender: 1,
  Receiver: 2,
  Cc: 3,
  Signatory: 4,
} as const

export const PARTICIPANT_TARGET_TYPE = {
  ExternalOrganization: 1,
  InternalDepartment: 2,
  Person: 3,
} as const

export const MOVEMENT_TYPE = {
  Referral: 1,
  Transfer: 2,
  Cc: 3,
  Return: 4,
} as const

export const MOVEMENT_STATUS = {
  Draft: 1,
  Sent: 2,
  Received: 3,
  InProgress: 4,
  Completed: 5,
  Rejected: 6,
  Returned: 7,
  Redirected: 8,
  Closed: 9,
  Canceled: 10,
} as const

export const MOVEMENT_STEP_STATUS = {
  Pending: 1,
  Sent: 2,
  Received: 3,
  Completed: 4,
  Rejected: 5,
  Returned: 6,
  Redirected: 7,
} as const

export const MOVEMENT_EVENT_TYPE = {
  Created: 1,
  Sent: 2,
  Received: 3,
  Completed: 4,
  Rejected: 5,
  Returned: 6,
  Redirected: 7,
  Closed: 8,
  Canceled: 9,
} as const

/**
 * Seeded document types that imply one direction; circulars and decisions use
 * `All directions`, so the form leaves their direction to the user.
 */
export const DOCUMENT_TYPE_DIRECTION: Readonly<Record<number, number>> = {
  [DOCUMENT_TYPE.IncomingLetter]: DOCUMENT_DIRECTION.Incoming,
  [DOCUMENT_TYPE.OutgoingLetter]: DOCUMENT_DIRECTION.Outgoing,
  [DOCUMENT_TYPE.InternalMemo]: DOCUMENT_DIRECTION.Internal,
}
