import type { PermissionCode } from '@/shared/config'

/** `RoleResponse` from `/api/roles` (camelCase JSON). */
export interface RoleResponse {
  roleId: string
  code: string
  nameAr: string
  nameEn: string | null
  description: string | null
  isSystemRole: boolean
  isActive: boolean
  createdAt: string
  permissions: PermissionCode[]
}

/** `PermissionResponse` from `/api/permissions`. */
export interface PermissionResponse {
  code: PermissionCode
  nameAr: string
  nameEn: string | null
  moduleName: string
  description: string | null
}
