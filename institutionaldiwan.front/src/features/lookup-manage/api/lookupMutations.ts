import { useMutation, useQueryClient } from '@tanstack/react-query'
import { httpClient } from '@/shared/api'
import { lookupKeys, type LookupEntryResponse } from '@/entities/lookup'
import type { CreateLookupEntryRequest, RelabelLookupEntryRequest } from '../model/types'

const base = (lookup: string) => `/lookups/${encodeURIComponent(lookup)}`

/** Lookup lists are cached forever, so every write drops all lookup queries. */
function useInvalidateLookups() {
  const queryClient = useQueryClient()
  return () => queryClient.invalidateQueries({ queryKey: lookupKeys.all })
}

/** `POST /api/lookups/{lookup}` — adds an entry with an administrator-chosen id. */
export function useCreateLookupEntryMutation() {
  const invalidate = useInvalidateLookups()
  return useMutation({
    mutationFn: async ({
      lookup,
      request,
    }: {
      lookup: string
      request: CreateLookupEntryRequest
    }): Promise<LookupEntryResponse> => {
      const { data } = await httpClient.post<LookupEntryResponse>(base(lookup), request)
      return data
    },
    onSuccess: invalidate,
  })
}

/** `PUT /api/lookups/{lookup}/{id}` — relabels an entry. */
export function useRelabelLookupEntryMutation() {
  const invalidate = useInvalidateLookups()
  return useMutation({
    mutationFn: async ({
      lookup,
      id,
      request,
    }: {
      lookup: string
      id: number
      request: RelabelLookupEntryRequest
    }): Promise<LookupEntryResponse> => {
      const { data } = await httpClient.put<LookupEntryResponse>(`${base(lookup)}/${id}`, request)
      return data
    },
    onSuccess: invalidate,
  })
}

/** `PATCH /api/lookups/{lookup}/{id}/deactivate|activate`. */
export function useSetLookupEntryActiveMutation() {
  const invalidate = useInvalidateLookups()
  return useMutation({
    mutationFn: async ({
      lookup,
      id,
      active,
    }: {
      lookup: string
      id: number
      active: boolean
    }): Promise<LookupEntryResponse> => {
      const { data } = await httpClient.patch<LookupEntryResponse>(
        `${base(lookup)}/${id}/${active ? 'activate' : 'deactivate'}`,
      )
      return data
    },
    onSuccess: invalidate,
  })
}

/** `PATCH /api/lookups/{lookup}/{id}/set-default|clear-default` — one default per catalog. */
export function useSetLookupEntryDefaultMutation() {
  const invalidate = useInvalidateLookups()
  return useMutation({
    mutationFn: async ({
      lookup,
      id,
      isDefault,
    }: {
      lookup: string
      id: number
      isDefault: boolean
    }): Promise<LookupEntryResponse> => {
      const { data } = await httpClient.patch<LookupEntryResponse>(
        `${base(lookup)}/${id}/${isDefault ? 'set-default' : 'clear-default'}`,
      )
      return data
    },
    onSuccess: invalidate,
  })
}
