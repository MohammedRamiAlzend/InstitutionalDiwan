import { useQuery } from '@tanstack/react-query'
import { httpClient } from '@/shared/api'
import { roleKeys } from './queries'

export interface RoleOption {
  roleId: string
  code: string
  nameAr: string
  isActive: boolean
}

/** Minimal role labels, including inactive assignments: USER_MANAGE OR ROLE_MANAGE. */
export function useRoleOptions(options?: { enabled?: boolean }) {
  return useQuery({
    queryKey: [...roleKeys.all, 'options'],
    queryFn: async ({ signal }) => (await httpClient.get<RoleOption[]>(
      '/identity-options/roles', { signal },
    )).data,
    ...options,
  })
}
