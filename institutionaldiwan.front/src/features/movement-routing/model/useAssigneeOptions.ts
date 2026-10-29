import { useMemo } from 'react'
import { useUserOptions } from '@/entities/user'
import { useDebouncedValue } from '@/shared/lib'
import type { SelectOption } from '@/shared/ui'

/** Minimal user picker options; `none` is the Radix-safe stand-in for "no assignee". */
export function useAssigneeOptions(search: string, enabled: boolean) {
  const debouncedSearch = useDebouncedValue(search)

  const query = useUserOptions(
    {
      page: 1,
      pageSize: 20,
      search: debouncedSearch.trim() || undefined,
    },
    { enabled },
  )

  const options = useMemo<SelectOption[]>(
    () => [
      { value: 'none', label: 'بدون إسناد' },
      ...(query.data?.items ?? [])
        .filter((user) => user.isActive)
        .map((user) => ({ value: user.userId, label: user.username })),
    ],
    [query.data],
  )

  return {
    options,
    isLoading: query.isLoading,
    isError: query.isError,
  }
}
