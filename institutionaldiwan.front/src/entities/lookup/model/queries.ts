import { useEffect } from 'react'
import { useQuery } from '@tanstack/react-query'
import { lookupApi } from '../api/lookupApi'
import { LOOKUP_FALLBACKS } from './constants'
import type { LookupEntryResponse } from './types'

export const lookupKeys = {
  all: ['lookups'] as const,
  catalogs: () => [...lookupKeys.all, 'catalogs'] as const,
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
            isDefault: false,
            createdAt: '',
          })),
  })
}

/** Slugs of all catalogs the server supports (the administration picker). */
export function useLookupCatalogs() {
  return useQuery({
    queryKey: lookupKeys.catalogs(),
    queryFn: () => lookupApi.catalogs(),
    staleTime: Infinity,
  })
}

/** The active entry flagged as the catalog default, or `undefined` when none is set. */
export function findDefaultLookupEntry(
  entries: readonly LookupEntryResponse[] | undefined,
): LookupEntryResponse | undefined {
  return entries?.find((entry) => entry.isDefault && entry.isActive)
}

/**
 * Preselects the catalog default in an empty select. Call it from create forms
 * only; `apply` receives the entry id as a string (the select value format).
 */
export function usePreselectLookupDefault(
  entries: readonly LookupEntryResponse[] | undefined,
  currentValue: string | undefined,
  apply: (value: string) => void,
  enabled = true,
) {
  const defaultId = findDefaultLookupEntry(entries)?.id
  useEffect(() => {
    if (enabled && !currentValue && defaultId !== undefined) apply(String(defaultId))
    // Only re-run when the default or the enabled flag changes, not on every keystroke.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [defaultId, enabled])
}
