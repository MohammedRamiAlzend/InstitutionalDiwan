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
}

/** Optional year filter for the department register book. */
export interface DepartmentRegisterParams {
  departmentId: string
  numberingYear?: number
}
