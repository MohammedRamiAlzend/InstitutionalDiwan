import { useMutation, useQueryClient, type QueryClient } from '@tanstack/react-query'
import { httpClient } from '@/shared/api'
import { roleKeys, type RoleResponse } from '@/entities/role'
import type {
  CreateRoleRequest,
  UpdateRolePermissionsRequest,
  UpdateRoleRequest,
} from '../model/types'

function invalidateRoles(queryClient: QueryClient, roleId?: string) {
  void queryClient.invalidateQueries({ queryKey: roleKeys.list() })
  if (roleId) {
    void queryClient.invalidateQueries({ queryKey: roleKeys.detail(roleId) })
  }
}

/** `POST /api/roles` — creates a non-system role. */
export function useCreateRoleMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (request: CreateRoleRequest): Promise<RoleResponse> => {
      const { data } = await httpClient.post<RoleResponse>('/roles', request)
      return data
    },
    onSuccess: () => invalidateRoles(queryClient),
  })
}

/** `PUT /api/roles/{id}` — display names and description; the code is immutable. */
export function useUpdateRoleMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      roleId,
      request,
    }: {
      roleId: string
      request: UpdateRoleRequest
    }): Promise<RoleResponse> => {
      const { data } = await httpClient.put<RoleResponse>(`/roles/${roleId}`, request)
      return data
    },
    onSuccess: (_data, variables) => invalidateRoles(queryClient, variables.roleId),
  })
}

/** `DELETE /api/roles/{id}` (deactivate) and `POST /api/roles/{id}/activate`. */
export function useSetRoleActiveMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      roleId,
      active,
    }: {
      roleId: string
      active: boolean
    }): Promise<RoleResponse> => {
      const { data } = active
        ? await httpClient.post<RoleResponse>(`/roles/${roleId}/activate`)
        : await httpClient.delete<RoleResponse>(`/roles/${roleId}`)
      return data
    },
    onSuccess: (_data, variables) => invalidateRoles(queryClient, variables.roleId),
  })
}

/**
 * Applies the matrix diff sequentially: grants first, then revokes. Invalidates
 * on settle so a mid-way failure still leaves the grid in sync with the server.
 */
export function useUpdateRolePermissionsMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      roleId,
      grant,
      revoke,
    }: UpdateRolePermissionsRequest): Promise<RoleResponse | null> => {
      let latest: RoleResponse | null = null

      for (const permissionCode of grant) {
        const { data } = await httpClient.post<RoleResponse>(
          `/roles/${roleId}/permissions`,
          { permissionCode },
        )
        latest = data
      }

      for (const permissionCode of revoke) {
        const { data } = await httpClient.delete<RoleResponse>(
          `/roles/${roleId}/permissions/${encodeURIComponent(permissionCode)}`,
        )
        latest = data
      }

      return latest
    },
    onSettled: (_data, _error, variables) =>
      invalidateRoles(queryClient, variables.roleId),
  })
}
