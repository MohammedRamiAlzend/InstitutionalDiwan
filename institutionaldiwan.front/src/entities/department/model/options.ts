import { useQuery } from '@tanstack/react-query'
import { httpClient } from '@/shared/api'
import { departmentKeys } from './queries'

export interface DepartmentOption {
  departmentId: string
  code: string
  nameAr: string
  isActive: boolean
}

/** Minimal labels: USER_MANAGE OR EMPLOYEE_MANAGE OR DEPARTMENT_MANAGE OR DOCUMENT_VIEW OR DOCUMENT_CREATE OR DOCUMENT_EDIT. */
export function useDepartmentOptions(options?: { enabled?: boolean }) {
  return useQuery({
    queryKey: [...departmentKeys.all, 'options'],
    queryFn: async ({ signal }) => (await httpClient.get<DepartmentOption[]>(
      '/identity-options/departments', { signal },
    )).data,
    ...options,
  })
}
