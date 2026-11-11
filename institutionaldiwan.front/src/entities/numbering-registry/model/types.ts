/** One per-year sequence counter attached to a registry. */
export interface NumberingCounterResponse {
  counterId: string
  numberingYear: number
  lastValue: number
  updatedAt: string
}

/** `NumberingRegistryResponse` — registry scope, number template, and counters. */
export interface NumberingRegistryResponse {
  registryId: string
  code: string
  nameAr: string
  departmentId: string | null
  documentTypeId: number | null
  directionId: number | null
  formatPattern: string
  resetPolicyId: number
  isActive: boolean
  createdAt: string
  counters: NumberingCounterResponse[]
}

/** One line in a department's own register book (`GET /api/departments/{id}/register`). */
export interface DepartmentRegisterLineResponse {
  documentNumberId: string
  documentId: string
  documentTitle: string
  centralNumberText: string | null
  registryNumberText: string
  numberingYear: number
  registryDate: string | null
  statusId: number
  registrationCount: number
  /** On a replaced line: the department's newer number for the same document. */
  replacedByNumberText: string | null
  /** On a newer line: the department's earlier number it replaced. */
  replacesNumberText: string | null
  /** The department register the line was written in; null only on a response from an older server. */
  registryId: string | null
}

/** Optional year filter for the department register book. */
export interface DepartmentRegisterParams {
  departmentId: string
  numberingYear?: number
}

/** One line of a department register page, with the record values of the chosen form version. */
export interface DepartmentRegisterEntryResponse {
  documentNumberId: string
  documentId: string
  registryId: string
  /** Empty when the caller may not view the document. */
  documentTitle: string
  centralNumberText: string | null
  registryNumberText: string
  numberingYear: number
  registryDate: string | null
  statusId: number
  registrationCount: number
  replacedByNumberText: string | null
  replacesNumberText: string | null
  /** The latest visit on the chosen form version; null when no version was chosen. */
  visitId: string | null
  recordLocked: boolean | null
  /** Stored text of each field of the chosen version, by field key. */
  values: Record<string, string | null>
}

/** Server-side narrowing of a department register page (`GET /api/departments/{id}/register/entries`). */
export interface DepartmentRegisterEntriesParams {
  departmentId: string
  registryId?: string
  numberingYear?: number
  statusId?: number
  /** `yyyy-mm-dd`, inclusive. */
  dateFrom?: string
  dateTo?: string
  schemaVersionId?: string
  page: number
  pageSize: number
  search?: string
  /** A column name, or `field:{key}` for a field of the chosen form version. */
  sortBy?: string
  sortDescending?: boolean
}
