import { httpClient } from '@/shared/api'
import type {
  DepartmentRegisterLineResponse,
  NumberingRegistryResponse,
} from '../model/types'

/** Typed reads for numbering registries and a department's own register book. */
export const numberingRegistryApi = {
  list: async (): Promise<NumberingRegistryResponse[]> => {
    const { data } = await httpClient.get<NumberingRegistryResponse[]>(
      '/numbering/registries',
    )
    return data
  },

  byId: async (registryId: string): Promise<NumberingRegistryResponse> => {
    const { data } = await httpClient.get<NumberingRegistryResponse>(
      `/numbering/registries/${registryId}`,
    )
    return data
  },

  departmentRegister: async (
    departmentId: string,
    numberingYear?: number,
  ): Promise<DepartmentRegisterLineResponse[]> => {
    const { data } = await httpClient.get<DepartmentRegisterLineResponse[]>(
      `/departments/${departmentId}/register`,
      { params: { numberingYear } },
    )
    return data
  },
}
