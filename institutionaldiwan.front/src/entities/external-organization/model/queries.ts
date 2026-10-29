import { useQuery, type UseQueryOptions } from '@tanstack/react-query'
import type { PagedResult } from '@/shared/types'
import { externalOrganizationApi } from '../api/externalOrganizationApi'
import type {
  ExternalOrganizationListParams,
  ExternalOrganizationResponse,
} from './types'

export const externalOrganizationKeys = {
  all: ['external-organizations'] as const,
  list: (params: ExternalOrganizationListParams) =>
    [...externalOrganizationKeys.all, 'list', params] as const,
  detail: (externalOrganizationId: string) =>
    [...externalOrganizationKeys.all, 'detail', externalOrganizationId] as const,
}

type EnabledOptions<T> = Partial<Pick<UseQueryOptions<T>, 'enabled'>>

/** Paged external correspondents, optionally narrowed by a code/name search. */
export function useExternalOrganizations(
  params: ExternalOrganizationListParams,
  options?: EnabledOptions<PagedResult<ExternalOrganizationResponse>>,
) {
  return useQuery({
    queryKey: externalOrganizationKeys.list(params),
    queryFn: () => externalOrganizationApi.list(params),
    staleTime: 5 * 60 * 1000,
    ...options,
  })
}

export function useExternalOrganizationById(
  externalOrganizationId: string | null | undefined,
  options?: EnabledOptions<ExternalOrganizationResponse>,
) {
  return useQuery({
    queryKey: externalOrganizationKeys.detail(externalOrganizationId ?? ''),
    queryFn: () =>
      externalOrganizationApi.byId(externalOrganizationId as string),
    staleTime: 5 * 60 * 1000,
    ...options,
    enabled: Boolean(externalOrganizationId) && (options?.enabled ?? true),
  })
}
