import type { PermissionCode } from '@/shared/config'

/** `POST /api/roles` payload; the server trims and upper-cases `code`. */
export interface CreateRoleRequest {
  code: string
  nameAr: string
  nameEn: string | null
  description: string | null
}

/** `PUT /api/roles/{id}` payload; the role code is immutable. */
export interface UpdateRoleRequest {
  nameAr: string
  nameEn: string | null
  description: string | null
}

/** Permission diff submitted by the matrix dialog. */
export interface UpdateRolePermissionsRequest {
  roleId: string
  grant: PermissionCode[]
  revoke: PermissionCode[]
}
