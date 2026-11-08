import { useQuery } from '@tanstack/react-query'
import { systemErrorApi } from '../api/systemErrorApi'
import type { SystemErrorListParams } from './types'

export const systemErrorKeys = {
  all: ['system-errors'] as const,
  list: (params: SystemErrorListParams) => [...systemErrorKeys.all, 'list', params] as const,
  details: (id: string) => [...systemErrorKeys.all, 'details', id] as const,
}

export function useSystemErrors(params: SystemErrorListParams) {
  return useQuery({
    queryKey: systemErrorKeys.list(params),
    queryFn: () => systemErrorApi.list(params),
    placeholderData: (previous) => previous,
  })
}

export function useSystemError(id: string | null) {
  return useQuery({
    queryKey: systemErrorKeys.details(id ?? ''),
    queryFn: () => systemErrorApi.details(id ?? ''),
    enabled: id !== null,
  })
}
