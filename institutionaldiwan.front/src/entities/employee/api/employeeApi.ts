import { httpClient } from '@/shared/api'
import type { PagedResult } from '@/shared/types'
import type { EmployeeListParams, EmployeeResponse } from '../model/types'

/** Typed read calls for employee records. */
export const employeeApi = {
  list: async (params: EmployeeListParams): Promise<PagedResult<EmployeeResponse>> => {
    const { data } = await httpClient.get<PagedResult<EmployeeResponse>>(
      '/employees',
      {
        params: {
          page: params.page,
          pageSize: params.pageSize,
          search: params.search?.trim() || undefined,
          sortBy: params.sortBy,
          sortDescending: params.sortDescending,
          departmentId: params.departmentId || undefined,
          includeSubDepartments: params.departmentId
            ? (params.includeSubDepartments ?? false)
            : undefined,
        },
      },
    )
    return data
  },

  byId: async (employeeId: string): Promise<EmployeeResponse> => {
    const { data } = await httpClient.get<EmployeeResponse>(
      `/employees/${employeeId}`,
    )
    return data
  },
}
