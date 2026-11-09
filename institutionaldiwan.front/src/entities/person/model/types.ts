/** A name offered while typing: only enough to tell two people apart. */
export interface PersonSuggestionResponse {
  personId: string
  displayName: string
  birthYear: number | null
  nationalNumberLast4: string | null
  createdAt: string
}

export interface PersonListItemResponse {
  personId: string
  displayName: string
  nationalNumber: string | null
  documentCount: number
  lastActivityAt: string
}

/** `GET /api/persons/{id}`. The screens show the name only for now; the rest is API-managed. */
export interface PersonResponse {
  personId: string
  displayName: string
  fullName: string | null
  firstName: string | null
  lastName: string | null
  fatherName: string | null
  motherName: string | null
  nationalNumber: string | null
  civilRegistryNumber: string | null
  civilRegistryPlace: string | null
  birthPlace: string | null
  birthDate: string | null
  genderId: number | null
  nationalityId: number | null
  phone: string | null
  address: string | null
  notes: string | null
  createdAt: string
  updatedAt: string | null
}

export interface DocumentPersonResponse {
  documentPersonId: string
  personId: string
  displayName: string
  roleId: number
  createdAt: string
}

export type PersonLastActionKind = 'STATUS' | 'MOVEMENT'

export interface PersonDocumentResponse {
  documentId: string
  title: string
  documentTypeId: number
  officialNumberText: string | null
  statusId: number
  owningDepartmentId: string
  documentPersonId: string
  roleId: number
  linkedAt: string
  currentDepartmentId: string | null
  currentArchiveLocationId: number | null
  activeMovementStatusId: number | null
  lastActionKind: PersonLastActionKind
  lastActionAt: string
  lastActionUserId: string
  lastStatusId: number | null
  lastMovementEventTypeId: number | null
}

export type PersonTimelineKind = 'STATUS' | 'MOVEMENT' | 'NUMBER' | 'ARCHIVE' | 'PERSON'

export interface PersonTimelineEntryResponse {
  kind: PersonTimelineKind
  occurredAt: string
  documentId: string
  documentTitle: string
  actorUserId: string
  fromStatusId: number | null
  toStatusId: number | null
  movementId: string | null
  movementEventTypeId: number | null
  fromDepartmentId: string | null
  toDepartmentId: string | null
  numberEventTypeId: number | null
  archiveEventTypeId: number | null
  archiveLocationId: number | null
  roleId: number | null
  notes: string | null
}

export interface PersonListParams {
  page: number
  pageSize: number
  search?: string
  sortBy?: string
  sortDescending?: boolean
}

export interface PersonDocumentListParams extends PersonListParams {
  roleId?: number
  statusId?: number
}

export interface PersonTimelineParams {
  page: number
  pageSize: number
}

/** One name in a person input: an existing person when `personId` is set, otherwise a new one. */
export interface PersonNameEntry {
  personId: string | null
  fullName: string
}
