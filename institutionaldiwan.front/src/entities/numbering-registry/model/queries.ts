import { keepPreviousData, useQuery, type UseQueryOptions } from '@tanstack/react-query'
import { numberingRegistryApi } from '../api/numberingRegistryApi'
import type { PagedResult } from '@/shared/types'
import type {
  DepartmentRegisterEntriesParams,
  DepartmentRegisterEntryResponse,
  DepartmentRegisterLineResponse,
  NumberingRegistryResponse,
} from './types'

export const numberingRegistryKeys = {
  all: ['numbering-registries'] as const,
  lists: () => [...numberingRegistryKeys.all, 'list'] as const,
  list: () => [...numberingRegistryKeys.lists()] as const,
  details: () => [...numberingRegistryKeys.all, 'detail'] as const,
  detail: (registryId: string) =>
    [...numberingRegistryKeys.details(), registryId] as const,
  registers: () => [...numberingRegistryKeys.all, 'department-register'] as const,
  entries: () => [...numberingRegistryKeys.all, 'register-entries'] as const,
  entriesPage: (params: DepartmentRegisterEntriesParams) =>
    [...numberingRegistryKeys.entries(), params] as const,
  register: (departmentId: string, numberingYear: number | null) =>
    [...numberingRegistryKeys.registers(), departmentId, numberingYear] as const,
}

type EnabledOptions<T> = Partial<Pick<UseQueryOptions<T>, 'enabled'>>

/** Every registry with its scope, template, and per-year counters (`NUMBERING_VIEW`). */
export function useNumberingRegistries(
  options?: EnabledOptions<NumberingRegistryResponse[]>,
) {
  return useQuery({
    queryKey: numberingRegistryKeys.list(),
    queryFn: numberingRegistryApi.list,
    staleTime: 30 * 1000,
    ...options,
  })
}

export function useNumberingRegistryById(
  registryId: string | null | undefined,
  options?: EnabledOptions<NumberingRegistryResponse>,
) {
  return useQuery({
    queryKey: numberingRegistryKeys.detail(registryId ?? ''),
    queryFn: () => numberingRegistryApi.byId(registryId as string),
    staleTime: 30 * 1000,
    ...options,
    enabled: Boolean(registryId) && (options?.enabled ?? true),
  })
}

/** A department's register book, optionally narrowed to one numbering year. */
export function useDepartmentRegister(
  departmentId: string | null | undefined,
  numberingYear: number | null,
  options?: EnabledOptions<DepartmentRegisterLineResponse[]>,
) {
  return useQuery({
    queryKey: numberingRegistryKeys.register(departmentId ?? '', numberingYear),
    queryFn: () =>
      numberingRegistryApi.departmentRegister(departmentId as string, numberingYear ?? undefined),
    staleTime: 30 * 1000,
    ...options,
    enabled: Boolean(departmentId) && (options?.enabled ?? true),
  })
}

/** One server-narrowed page of a department's register; the previous page stays while the next loads. */
export function useDepartmentRegisterEntries(
  params: DepartmentRegisterEntriesParams | null,
  options?: EnabledOptions<PagedResult<DepartmentRegisterEntryResponse>>,
) {
  return useQuery({
    queryKey: numberingRegistryKeys.entriesPage(
      params ?? { departmentId: '', page: 1, pageSize: 1 },
    ),
    queryFn: () => numberingRegistryApi.departmentRegisterEntries(params as DepartmentRegisterEntriesParams),
    placeholderData: keepPreviousData,
    staleTime: 15 * 1000,
    enabled: params !== null && (options?.enabled ?? true),
  })
}
