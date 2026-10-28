import { useQuery, type UseQueryOptions } from '@tanstack/react-query'
import { departmentApi } from '../api/departmentApi'
import type { DepartmentResponse, DepartmentTreeNodeResponse } from './types'

export const departmentKeys = {
  all: ['departments'] as const,
  list: () => [...departmentKeys.all, 'list'] as const,
  tree: () => [...departmentKeys.all, 'tree'] as const,
  detail: (departmentId: string) =>
    [...departmentKeys.all, 'detail', departmentId] as const,
}

type EnabledOptions<T> = Partial<Pick<UseQueryOptions<T>, 'enabled'>>

/** Flat department list, ordered by code (requires `DEPARTMENT_MANAGE`). */
export function useDepartments(options?: EnabledOptions<DepartmentResponse[]>) {
  return useQuery({
    queryKey: departmentKeys.list(),
    queryFn: departmentApi.list,
    staleTime: 5 * 60 * 1000,
    ...options,
  })
}

/** Department hierarchy as root nodes (requires `DEPARTMENT_MANAGE`). */
export function useDepartmentTree(
  options?: EnabledOptions<DepartmentTreeNodeResponse[]>,
) {
  return useQuery({
    queryKey: departmentKeys.tree(),
    queryFn: departmentApi.tree,
    staleTime: 5 * 60 * 1000,
    ...options,
  })
}

export function useDepartmentById(
  departmentId: string | null | undefined,
  options?: EnabledOptions<DepartmentResponse>,
) {
  return useQuery({
    queryKey: departmentKeys.detail(departmentId ?? ''),
    queryFn: () => departmentApi.byId(departmentId as string),
    staleTime: 5 * 60 * 1000,
    ...options,
    enabled: Boolean(departmentId) && (options?.enabled ?? true),
  })
}
