import type { PermissionCode } from '@/shared/config'

/** `GET /api/auth/me` — caller profile projected from the JWT (camelCase JSON). */
export interface MeResponse {
  userId: string
  username: string
  roles: string[]
  permissions: PermissionCode[]
}

/** Account visibility scope (Module 02 `ScopeTypeId` rendered semantically). */
export type UserScope = 'Department' | 'DepartmentTree' | 'Institution' | 'System'

/** `UserResponse` from `/api/users` (camelCase JSON). */
export interface UserResponse {
  userId: string
  username: string
  employeeId: string | null
  primaryDepartmentId: string
  scopeTypeId: number
  isActive: boolean
  mustChangePassword: boolean
  lastLoginAt: string | null
  createdAt: string
  updatedAt: string | null
  roles: string[]
  departmentAssignments: UserDepartmentAssignmentResponse[]
}

/** Server-side paging + username search for the user list. */
export interface UserListParams {
  page: number
  pageSize: number
  search?: string
  sortBy?: string
  sortDescending?: boolean
}

/** `GET /api/identity-options/users` — minimal picker row (no contact details, roles, or scope). */
export interface UserOption {
  userId: string
  username: string
  isActive: boolean
}

/** Paging + username search for the minimal user picker. */
export interface UserOptionParams {
  page: number
  pageSize: number
  search?: string
}

/** One temporal department assignment held by a user. */
export interface UserDepartmentAssignmentResponse {
  assignmentId: string
  departmentId: string
  assignmentTypeId: number
  includeSubtree: boolean
  startDate: string
  endDate: string | null
  isActive: boolean
}

/** `GET /api/users/{id}/effective-permissions` — same set the access token carries. */
export interface UserEffectivePermissionsResponse {
  userId: string
  username: string
  scopeTypeId: number
  primaryDepartmentId: string
  roleCodes: string[]
  permissions: PermissionCode[]
  departmentAssignments: UserDepartmentAssignmentResponse[]
}

/** The authenticated caller as held by the session store. */
export interface SessionUser {
  userId: string
  username: string
  roles: string[]
  permissions: PermissionCode[]
}
