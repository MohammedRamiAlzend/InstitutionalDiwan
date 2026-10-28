import { httpClient } from '@/shared/api'
import type { PermissionResponse, RoleResponse } from '../model/types'

/** Typed read calls for roles and the permission catalog. */
export const roleApi = {
  list: async (): Promise<RoleResponse[]> => {
    const { data } = await httpClient.get<RoleResponse[]>('/roles')
    return data
  },

  byId: async (roleId: string): Promise<RoleResponse> => {
    const { data } = await httpClient.get<RoleResponse>(`/roles/${roleId}`)
    return data
  },

  permissions: async (): Promise<PermissionResponse[]> => {
    const { data } = await httpClient.get<PermissionResponse[]>('/permissions')
    return data
  },
}
