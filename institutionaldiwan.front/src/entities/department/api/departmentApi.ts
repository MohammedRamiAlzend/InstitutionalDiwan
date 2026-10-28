import { httpClient } from '@/shared/api'
import type { DepartmentResponse, DepartmentTreeNodeResponse } from '../model/types'

/** Typed read calls for the organization departments. */
export const departmentApi = {
  list: async (): Promise<DepartmentResponse[]> => {
    const { data } = await httpClient.get<DepartmentResponse[]>('/departments')
    return data
  },

  tree: async (): Promise<DepartmentTreeNodeResponse[]> => {
    const { data } = await httpClient.get<DepartmentTreeNodeResponse[]>(
      '/departments/tree',
    )
    return data
  },

  byId: async (departmentId: string): Promise<DepartmentResponse> => {
    const { data } = await httpClient.get<DepartmentResponse>(
      `/departments/${departmentId}`,
    )
    return data
  },
}
