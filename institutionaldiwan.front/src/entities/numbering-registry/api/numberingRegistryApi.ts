import { httpClient } from '@/shared/api'
import type { PagedResult } from '@/shared/types'
import type {
  DepartmentRegisterEntriesParams,
  DepartmentRegisterEntryResponse,
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
  /** One page of a department's register, narrowed and ordered by the server. */
  departmentRegisterEntries: async ({
    departmentId,
    search,
    fieldFilters,
    ...params
  }: DepartmentRegisterEntriesParams): Promise<PagedResult<DepartmentRegisterEntryResponse>> => {
    const { data } = await httpClient.get<PagedResult<DepartmentRegisterEntryResponse>>(
      `/departments/${departmentId}/register/entries`,
      {
        params: {
          ...params,
          search: search?.trim() || undefined,
          fieldFilters: fieldFilters?.length ? fieldFilters : undefined,
        },
        // ASP.NET binds repeated keys (`fieldFilters=a&fieldFilters=b`), not `fieldFilters[]`.
        paramsSerializer: { indexes: null },
      },
    )
    return data
  },
}
