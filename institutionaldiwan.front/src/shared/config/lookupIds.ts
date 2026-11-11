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

/** `DocumentNumberStatus` seeds — the lifecycle of an issued official number. */
export const DOCUMENT_NUMBER_STATUS = {
  Active: 1,
  Cancelled: 2,
  Replaced: 3,
} as const

/** `DocumentNumberEventType` seeds — rows in a number's immutable history. */
export const DOCUMENT_NUMBER_EVENT_TYPE = {
  Issued: 1,
  Cancelled: 2,
  Replaced: 3,
  RegisteredInDepartment: 4,
  ReRegisteredInDepartment: 5,
} as const

export const DOCUMENT_DIRECTION = {
  Internal: 1,
  Incoming: 2,
  Outgoing: 3,
  All: 4,
} as const

export const DOCUMENT_TYPE = {
  BaseRecord: 1,
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

export const ORGANIZATION_TYPE = {
  Government: 1,
  Private: 2,
  International: 3,
  Individual: 4,
} as const

export const MOVEMENT_TYPE = {
  Referral: 1,
  Transfer: 2,
  Cc: 3,
  Return: 4,
  /** On its way to the archive diwan, through the department that created the document. */
  ToArchive: 5,
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

/** `DocumentFileType` seeds (Module 06) — the file-kind picker of the upload form. */
export const DOCUMENT_FILE_TYPE = {
  Scan: 1,
  Attachment: 2,
  Signature: 3,
  Other: 4,
  Decision: 5,
} as const

export const DECISION_EFFECT = {
  Refer: 1,
  SendToArchive: 2,
  Join: 3,
  None: 4,
} as const

export const DECISION_STATUS = {
  Issued: 1,
  Appealed: 2,
  Final: 3,
  Revoked: 4,
  Canceled: 5,
} as const

/** `AppealOutcome`. Pending is recorded with the appeal; a ruling is accepted or rejected. */
export const APPEAL_OUTCOME = {
  Pending: 1,
  Accepted: 2,
  Rejected: 3,
} as const

export const AUDIT_ACTION_TYPE = {
  Create: 1,
  Update: 2,
  Delete: 3,
  View: 4,
  Login: 5,
  Logout: 6,
  Approve: 7,
  Execute: 8,
} as const

export const AUDIT_ENTITY_TYPE = {
  Document: 1,
  Department: 2,
  User: 3,
  Role: 4,
  Movement: 5,
  File: 6,
  NumberingRegistry: 7,
  Setting: 8,
  AuditLog: 9,
  DocumentNumber: 10,
  DocumentRelation: 11,
  ExternalOrganization: 12,
  Operation: 13,
} as const


/** `archive-event-types` — mirrors `ArchiveEventType` in the Domain. */
export const ARCHIVE_EVENT_TYPE = {
  Archived: 1,
  Unarchived: 2,
  RetentionApplied: 3,
} as const

/** `archive-locations` — mirrors `ArchiveLocation` in the Domain. */
export const ARCHIVE_LOCATION = {
  CentralRegistry: 1,
  PhysicalArchive: 2,
  ElectronicArchive: 3,
} as const

/** `document-relation-types` — mirrors `DocumentRelationType` in the Domain. */
export const DOCUMENT_RELATION_TYPE = {
  ReplyTo: 1,
  Related: 2,
  Supersedes: 3,
  DuplicateOf: 4,
  JoinedInto: 5,
} as const

/** `relation-action-types` — mirrors `RelationActionType` in the Domain. */
export const RELATION_ACTION_TYPE = {
  Created: 1,
  Updated: 2,
  Removed: 3,
} as const

/** `notification-types` — mirrors `NotificationType` in the Domain. */
export const NOTIFICATION_TYPE = {
  DocumentCreated: 1,
  DocumentRegistered: 2,
  MovementSent: 3,
  MovementReceived: 4,
  MovementCompleted: 5,
  DocumentArchived: 6,
  MovementRejected: 7,
  MovementReturned: 8,
  MovementRedirected: 9,
  MovementClosed: 10,
  MovementCancelled: 11,
} as const

/** `notification-priorities` — mirrors `NotificationPriority` in the Domain. */
export const NOTIFICATION_PRIORITY = {
  Normal: 1,
  High: 2,
  Urgent: 3,
} as const

/** `sortBy` values `GET /api/notifications` accepts; anything else keeps newest first. */
export const NOTIFICATION_SORT_KEY = {
  DeliveredAt: 'deliveredAt',
  Priority: 'priority',
  Type: 'type',
  IsRead: 'isRead',
} as const
