import { useMemo } from 'react'
import { useNumberingRegistries } from './queries'
import type { NumberingRegistryResponse } from './types'

export interface DepartmentRegistries {
  registries: NumberingRegistryResponse[]
  isLoading: boolean
  /** The list loaded and the department has no active register. */
  isEmpty: boolean
}

/** The department's active registers, as full rows. */
export function useDepartmentRegistries(
  departmentId: string | null | undefined,
  { enabled = true }: { enabled?: boolean } = {},
): DepartmentRegistries {
  const query = useNumberingRegistries({ enabled: enabled && Boolean(departmentId) })

  const registries = useMemo(
    () =>
      (query.data ?? []).filter(
        (registry) => registry.isActive && registry.departmentId === departmentId,
      ),
    [query.data, departmentId],
  )

  return {
    registries,
    isLoading: query.isLoading,
    isEmpty: query.isSuccess && registries.length === 0,
  }
}

export interface DepartmentRegistryOptions {
  options: { value: string; label: string }[]
  isLoading: boolean
  /** The list loaded and the department has no active register. */
  isEmpty: boolean
}

/** The department's active registers, as options for choosing where a number is written. */
export function useDepartmentRegistryOptions(
  departmentId: string | null | undefined,
  { enabled = true }: { enabled?: boolean } = {},
): DepartmentRegistryOptions {
  const { registries, isLoading, isEmpty } = useDepartmentRegistries(departmentId, { enabled })

  const options = useMemo(
    () =>
      registries.map((registry) => ({
        value: registry.registryId,
        label: `${registry.code} — ${registry.nameAr}`,
      })),
    [registries],
  )

  return { options, isLoading, isEmpty }
}