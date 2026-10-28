/** `DepartmentResponse` from `/api/departments` (camelCase JSON). */
export interface DepartmentResponse {
  departmentId: string
  code: string
  nameAr: string
  nameEn: string | null
  departmentTypeId: number
  parentDepartmentId: string | null
  managerEmployeeId: string | null
  isActive: boolean
  canSendExternal: boolean
  canReceiveExternal: boolean
  notes: string | null
  createdAt: string
  updatedAt: string | null
}

/** `DepartmentTreeNodeResponse` from `/api/departments/tree`. */
export interface DepartmentTreeNodeResponse {
  departmentId: string
  code: string
  nameAr: string
  nameEn: string | null
  departmentTypeId: number
  parentDepartmentId: string | null
  isActive: boolean
  children: DepartmentTreeNodeResponse[]
}
