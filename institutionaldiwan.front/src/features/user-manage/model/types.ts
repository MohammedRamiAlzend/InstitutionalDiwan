/** Request bodies for the user administration commands (`/api/users`). */
export interface CreateUserRequest {
  username: string
  password: string
  primaryDepartmentId: string
  employeeId: string | null
  scopeTypeId: number
  mustChangePassword: boolean
}

export interface UpdateUserRequest {
  primaryDepartmentId: string
  employeeId: string | null
  scopeTypeId: number
  mustChangePassword: boolean
}

/** `POST /api/users/{id}/reset-password` — temporary password set by the administrator. */
export interface ResetUserPasswordRequest {
  newPassword: string
}

export interface AssignDepartmentScopeRequest {
  departmentId: string
  assignmentTypeId: number
  includeSubtree: boolean
  startDate: string | null
  endDate: string | null
}
