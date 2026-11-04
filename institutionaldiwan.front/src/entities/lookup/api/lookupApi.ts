import { httpClient } from '@/shared/api'
import type { LookupEntryResponse } from '../model/types'

/** Typed read calls for the reference lookup catalogs. */
export const lookupApi = {
  /** `GET /api/lookups` — slugs of every supported catalog. */
  catalogs: async (): Promise<string[]> => {
    const { data } = await httpClient.get<string[]>('/lookups')
    return data
  },
  entries: async (
    lookup: string,
    includeInactive = false,
  ): Promise<LookupEntryResponse[]> => {
    const { data } = await httpClient.get<LookupEntryResponse[]>(
      `/lookups/${encodeURIComponent(lookup)}`,
      { params: includeInactive ? { includeInactive } : undefined },
    )
    return data
  },
}
