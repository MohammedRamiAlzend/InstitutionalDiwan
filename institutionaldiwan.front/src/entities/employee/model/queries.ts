import { useQuery, type UseQueryOptions } from '@tanstack/react-query'
import type { PagedResult } from '@/shared/types'
import { employeeApi } from '../api/employeeApi'
import type { EmployeeListParams, EmployeeResponse } from './types'

export const employeeKeys = {
  all: ['employees'] as const,
  lists: () => [...employeeKeys.all, 'list'] as const,
  list: (params: EmployeeListParams) => [...employeeKeys.lists(), params] as const,
  detail: (employeeId: string) =>
    [...employeeKeys.all, 'detail', employeeId] as const,
}

type EnabledOptions<T> = Partial<Pick<UseQueryOptions<T>, 'enabled'>>

/** Paged employees with name/number search (requires `EMPLOYEE_MANAGE`). */
export function useEmployees(
  params: EmployeeListParams,
  options?: EnabledOptions<PagedResult<EmployeeResponse>>,
) {
  return useQuery({
    queryKey: employeeKeys.list(params),
    queryFn: () => employeeApi.list(params),
    placeholderData: (previous) => previous,
    staleTime: 60 * 1000,
    ...options,
  })
}

export function useEmployeeById(
  employeeId: string | null | undefined,
  options?: EnabledOptions<EmployeeResponse>,
) {
  return useQuery({
    queryKey: employeeKeys.detail(employeeId ?? ''),
    queryFn: () => employeeApi.byId(employeeId as string),
    staleTime: 5 * 60 * 1000,
    ...options,
    enabled: Boolean(employeeId) && (options?.enabled ?? true),
  })
}
