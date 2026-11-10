/** `DocumentFieldDataType` — the API serializes enums as integers. */
export const FIELD_DATA_TYPE = {
  Text: 1,
  LongText: 2,
  Number: 3,
  Date: 4,
  Boolean: 5,
  Select: 6,
  MultiSelect: 7,
} as const

export type FieldDataType = (typeof FIELD_DATA_TYPE)[keyof typeof FIELD_DATA_TYPE]

/** `DocumentTypeSchemaVersionStatus`. */
export const SCHEMA_STATUS = {
  Draft: 1,
  Published: 2,
  Retired: 3,
} as const

export type SchemaStatus = (typeof SCHEMA_STATUS)[keyof typeof SCHEMA_STATUS]

/** `DocumentFieldOptionSource` — where the choices of a select or multi-select field come from. */
export const FIELD_OPTION_SOURCE = {
  Static: 0,
  Departments: 1,
  ExternalOrganizations: 2,
  Lookup: 3,
} as const

export type FieldOptionSource = (typeof FIELD_OPTION_SOURCE)[keyof typeof FIELD_OPTION_SOURCE]

/** Stands for the current date in a default value or a date limit. */
export const TODAY_TOKEN = '@today'

/** Limits mirrored from the backend rules (DomainErrors.DocumentFields). */
export const FIELD_LIMITS = {
  maxFieldsPerVersion: 50,
  maxOptions: 100,
  maxRegexLength: 500,
  maxRegexMessageLength: 500,
  maxLabelLength: 200,
  maxKeyLength: 50,
  maxTextLength: 500,
  maxLongTextLength: 4000,
  maxPlaceholderLength: 200,
  maxHelpTextLength: 500,
  maxSectionTitleLength: 100,
  maxSelections: 50,
} as const

/** `DocumentFieldResponse` — the stable identity of a field of a document type. */
export interface DocumentFieldResponse {
  documentFieldId: string
  documentTypeId: number
  key: string
  dataType: FieldDataType
  createdAt: string
  /** Set for a field of a department record form; null (or absent on an older API) for the creation form. */
  departmentId?: string | null
}

export interface FieldOption {
  key: string
  labelAr: string
  /** False only for a sourced choice whose entry was deactivated: readable, no longer offered. */
  isActive?: boolean
}

/** `DocumentFieldBehavior` — how a field behaves in forms and document tables. */
export interface FieldBehavior {
  defaultValue: string | null
  isReadOnly: boolean
  isFilterable: boolean
  showInTable: boolean
  placeholderAr: string | null
  helpTextAr: string | null
  minLength: number | null
  minDate: string | null
  maxDate: string | null
  visibleWhenFieldKey: string | null
  visibleWhenValue: string | null
  isHalfWidth: boolean
  sectionTitleAr: string | null
  optionSource: FieldOptionSource
  lookupCatalog: string | null
}

/** `SchemaFieldResponse` — a field as configured inside one schema version. */
export interface SchemaFieldResponse {
  documentFieldId: string
  key: string
  dataType: FieldDataType
  labelAr: string
  isRequired: boolean
  displayOrder: number
  regexPattern: string | null
  regexErrorMessageAr: string | null
  minNumber: number | null
  maxNumber: number | null
  maxLength: number | null
  options: FieldOption[]
  behavior: FieldBehavior
}

/** `SchemaVersionResponse` — one version with its fields in display order. */
export interface SchemaVersionResponse {
  schemaVersionId: string
  documentTypeId: number
  versionNumber: number
  status: SchemaStatus
  createdAt: string
  publishedAt: string | null
  publishedByUserId: string | null
  fields: SchemaFieldResponse[]
  /** Set for a department record form; null (or absent on an older API) for the creation form. */
  departmentId?: string | null
}

/** `SchemaVersionSummaryResponse` — a history row without its fields. */
export interface SchemaVersionSummaryResponse {
  schemaVersionId: string
  versionNumber: number
  status: SchemaStatus
  fieldCount: number
  createdAt: string
  publishedAt: string | null
  publishedByUserId: string | null
  departmentId?: string | null
}

/** `DocumentTypeSchemaResponse` — the published schema a new document is pinned to, if any. */
export interface DocumentTypeSchemaResponse {
  documentTypeId: number
  schema: SchemaVersionResponse | null
}

export interface DocumentFieldValueResponse {
  field: SchemaFieldResponse
  value: string | null
}

/** `DocumentFieldsResponse` — a document's pinned schema version and stored values. */
export interface DocumentFieldsResponse {
  documentId: string
  schemaVersionId: string | null
  versionNumber: number | null
  fields: DocumentFieldValueResponse[]
}

/**
 * `DepartmentRecordVisitResponse` — one arrival of the document in a department and the record the
 * department kept on it. `fields` is empty when the department had no published form then.
 */
export interface DepartmentRecordVisitResponse {
  visitId: string
  registryDate: string
  movementStepId: string | null
  recordedAt: string
  schemaVersionId: string | null
  versionNumber: number | null
  isLocked: boolean
  lockedAt: string | null
  recordUpdatedAt: string | null
  correctedAt: string | null
  correctionReason: string | null
  /** The caller may edit this open record (own department, `DOCUMENT_EDIT`). */
  canEdit: boolean
  /** The caller may correct this locked record (`DEPARTMENT_RECORD_CORRECT`). */
  canCorrect: boolean
  fields: DocumentFieldValueResponse[]
}

/** `DepartmentRecordResponse` — one department's register line on the document and its visits. */
export interface DepartmentRecordResponse {
  registryNumberId: string
  departmentId: string
  departmentNameAr: string
  registryNumberText: string
  numberingYear: number
  visits: DepartmentRecordVisitResponse[]
}

/** `DocumentDepartmentRecordsResponse` — only the records the caller may read. */
export interface DocumentDepartmentRecordsResponse {
  documentId: string
  records: DepartmentRecordResponse[]
}
