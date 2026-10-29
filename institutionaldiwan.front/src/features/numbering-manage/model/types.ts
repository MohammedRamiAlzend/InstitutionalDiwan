/** `POST /api/numbering/registries` — code is normalized to uppercase by the server. */
export interface CreateNumberingRegistryRequest {
  code: string
  nameAr: string
  formatPattern: string | null
  resetPolicyId: number
  departmentId: string | null
  documentTypeId: number | null
  directionId: number | null
}

/** `PUT /api/numbering/registries/{registryId}` — code is immutable. */
export interface UpdateNumberingRegistryRequest {
  nameAr: string
  formatPattern: string | null
  resetPolicyId: number
  departmentId: string | null
  documentTypeId: number | null
  directionId: number | null
}

/** `POST /api/documents/{documentId}/numbers/issue` — draft documents only. */
export interface IssueOfficialNumberRequest {
  numberingYear: number | null
}

export interface CancelOfficialNumberRequest {
  reason: string
}

export interface ReplaceOfficialNumberRequest {
  reason: string
  numberingYear: number | null
}

/** `POST /api/documents/{documentId}/numbers/department-registration`. */
export interface RecordDepartmentRegistrationRequest {
  departmentId: string
  registryNumberText: string
  registryDate: string
  notes: string | null
}
