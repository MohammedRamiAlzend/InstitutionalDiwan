/** `POST /api/departments` payload; empty optional text is sent as `null`. */
export interface CreateDepartmentRequest {
  code: string
  nameAr: string
  departmentTypeId: number
  parentDepartmentId: string | null
  nameEn: string | null
  canSendExternal: boolean
  canReceiveExternal: boolean
  notes: string | null
}

/** `PUT /api/departments/{id}` payload; the code and department type are immutable. */
export interface UpdateDepartmentRequest {
  nameAr: string
  parentDepartmentId: string | null
  canSendExternal: boolean
  canReceiveExternal: boolean
  nameEn: string | null
  /** `null` clears the assigned manager. */
  managerEmployeeId: string | null
  notes: string | null
}
