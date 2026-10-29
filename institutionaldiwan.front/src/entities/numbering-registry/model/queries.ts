import { useQuery, type UseQueryOptions } from '@tanstack/react-query'
import { numberingRegistryApi } from '../api/numberingRegistryApi'
import type {
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
