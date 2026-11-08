import { useMemo } from 'react'
import { useNumberingRegistries } from './queries'

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
  const query = useNumberingRegistries({ enabled: enabled && Boolean(departmentId) })

  const options = useMemo(
    () =>
      (query.data ?? [])
        .filter((registry) => registry.isActive && registry.departmentId === departmentId)
        .map((registry) => ({
          value: registry.registryId,
          label: `${registry.code} — ${registry.nameAr}`,
        })),
    [query.data, departmentId],
  )

  return { options, isLoading: query.isLoading, isEmpty: query.isSuccess && options.length === 0 }
}
