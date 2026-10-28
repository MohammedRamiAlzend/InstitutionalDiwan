import { useQuery, type UseQueryOptions } from '@tanstack/react-query'
import { roleApi } from '../api/roleApi'
import type { PermissionResponse, RoleResponse } from './types'

export const roleKeys = {
  all: ['roles'] as const,
  list: () => [...roleKeys.all, 'list'] as const,
  detail: (roleId: string) => [...roleKeys.all, 'detail', roleId] as const,
}

export const permissionKeys = {
  all: ['permissions'] as const,
  list: () => [...permissionKeys.all, 'list'] as const,
}

type EnabledOptions<T> = Partial<Pick<UseQueryOptions<T>, 'enabled'>>

/** Every role with its granted permission codes (requires `ROLE_MANAGE`). */
export function useRoles(options?: EnabledOptions<RoleResponse[]>) {
  return useQuery({
    queryKey: roleKeys.list(),
    queryFn: roleApi.list,
    staleTime: 5 * 60 * 1000,
    ...options,
  })
}

export function useRoleById(
  roleId: string | null | undefined,
  options?: EnabledOptions<RoleResponse>,
) {
  return useQuery({
    queryKey: roleKeys.detail(roleId ?? ''),
    queryFn: () => roleApi.byId(roleId as string),
    staleTime: 5 * 60 * 1000,
    ...options,
    enabled: Boolean(roleId) && (options?.enabled ?? true),
  })
}

/** The 16-code permission catalog with Arabic names and module grouping. */
export function usePermissionsCatalog(
  options?: EnabledOptions<PermissionResponse[]>,
) {
  return useQuery({
    queryKey: permissionKeys.list(),
    queryFn: roleApi.permissions,
    staleTime: Infinity,
    ...options,
  })
}
