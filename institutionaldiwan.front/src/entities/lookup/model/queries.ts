import { useQuery } from '@tanstack/react-query'
import { lookupApi } from '../api/lookupApi'
import { LOOKUP_FALLBACKS } from './constants'
import type { LookupEntryResponse } from './types'

export const lookupKeys = {
  all: ['lookups'] as const,
  catalog: (lookup: string, includeInactive: boolean) =>
    [...lookupKeys.all, lookup, { includeInactive }] as const,
}

interface UseLookupEntriesOptions {
  includeInactive?: boolean
  enabled?: boolean
}

/**
 * Loads one reference catalog. Seed data is used as placeholder data so labels
 * render immediately (and stay usable when the caller lacks the lookup
 * permission); a successful response replaces it.
 */
export function useLookupEntries(
  lookup: string,
  options: UseLookupEntriesOptions = {},
) {
  const { includeInactive = false, enabled = true } = options

  return useQuery({
    queryKey: lookupKeys.catalog(lookup, includeInactive),
    queryFn: () => lookupApi.entries(lookup, includeInactive),
    staleTime: Infinity,
    enabled,
    placeholderData: includeInactive
      ? undefined
      : (): LookupEntryResponse[] | undefined =>
          LOOKUP_FALLBACKS[lookup]?.map((entry) => ({
            ...entry,
            isActive: true,
            createdAt: '',
          })),
  })
}
