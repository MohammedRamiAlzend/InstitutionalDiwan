import type { PermissionCode } from '@/shared/config'

/** `GET /api/auth/me` — caller profile projected from the JWT (camelCase JSON). */
export interface MeResponse {
  userId: string
  username: string
  roles: string[]
  permissions: PermissionCode[]
}

/** Account visibility scope (Module 02 `ScopeTypeId` rendered semantically). */
export type UserScope = 'System' | 'Department' | 'Subtree'

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
}

/** `EmployeeResponse` from `/api/employees` (camelCase JSON). */
export interface EmployeeResponse {
  employeeId: string
  employeeNumber: string | null
  fullNameAr: string
  fullNameEn: string | null
  departmentId: string
  jobTitleAr: string | null
  email: string | null
  phone: string | null
  isActive: boolean
  createdAt: string
  updatedAt: string | null
}

/** The authenticated caller as held by the session store. */
export interface SessionUser {
  userId: string
  username: string
  roles: string[]
  permissions: PermissionCode[]
}
