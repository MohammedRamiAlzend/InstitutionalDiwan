import { useQuery } from '@tanstack/react-query'
import { httpClient } from '@/shared/api'
import type { PagedResult } from '@/shared/types'
import { employeeKeys } from './queries'

export interface EmployeeOption {
  employeeId: string
  fullNameAr: string
  employeeNumber: string | null
  isActive: boolean
}

/** Minimal picker reads: USER_MANAGE OR DEPARTMENT_MANAGE OR EMPLOYEE_MANAGE. */
export function useEmployeeOptions(
  params: { page: number; pageSize: number; search?: string },
  options?: { enabled?: boolean },
) {
  return useQuery({
    queryKey: [...employeeKeys.all, 'options', params],
    queryFn: async ({ signal }) => (await httpClient.get<PagedResult<EmployeeOption>>(
      '/identity-options/employees', { params, signal },
    )).data,
    ...options,
  })
}

export function useEmployeeOptionById(
  employeeId: string | null | undefined,
  options?: { enabled?: boolean },
) {
  return useQuery({
    queryKey: [...employeeKeys.all, 'option', employeeId],
    queryFn: async ({ signal }) => (await httpClient.get<EmployeeOption>(
      `/identity-options/employees/${employeeId}`, { signal },
    )).data,
    enabled: Boolean(employeeId) && (options?.enabled ?? true),
  })
}
