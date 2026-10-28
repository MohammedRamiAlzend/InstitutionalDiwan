import { useQuery, type UseQueryOptions } from '@tanstack/react-query'
import type { PagedResult } from '@/shared/types'
import { documentApi } from '../api/documentApi'
import type {
  DocumentAccessGrantResponse,
  DocumentDetailResponse,
  DocumentListParams,
  DocumentResponse,
  DocumentTimelineEntryResponse,
} from './types'

export const documentKeys = {
  all: ['documents'] as const,
  lists: () => [...documentKeys.all, 'list'] as const,
  list: (params: DocumentListParams) => [...documentKeys.lists(), params] as const,
  details: () => [...documentKeys.all, 'detail'] as const,
  detail: (documentId: string) => [...documentKeys.details(), documentId] as const,
  full: (documentId: string) => [...documentKeys.detail(documentId), 'full'] as const,
  timeline: (documentId: string) => [...documentKeys.detail(documentId), 'timeline'] as const,
  grants: (documentId: string) => [...documentKeys.detail(documentId), 'grants'] as const,
}

type EnabledOptions<T> = Partial<Pick<UseQueryOptions<T>, 'enabled'>>

/** Paged document registry with search, sorting, and status/direction/type/department filters. */
export function useDocuments(
  params: DocumentListParams,
  options?: EnabledOptions<PagedResult<DocumentResponse>>,
) {
  return useQuery({
    queryKey: documentKeys.list(params),
    queryFn: () => documentApi.list(params),
    placeholderData: (previous) => previous,
    staleTime: 30 * 1000,
    ...options,
  })
}

export function useDocumentById(
  documentId: string | null | undefined,
  options?: EnabledOptions<DocumentResponse>,
) {
  return useQuery({
    queryKey: documentKeys.detail(documentId ?? ''),
    queryFn: () => documentApi.byId(documentId as string),
    staleTime: 60 * 1000,
    ...options,
    enabled: Boolean(documentId) && (options?.enabled ?? true),
  })
}

/** Document plus participants and status history, for the detail page. */
export function useDocumentDetails(
  documentId: string | null | undefined,
  options?: EnabledOptions<DocumentDetailResponse>,
) {
  return useQuery({
    queryKey: documentKeys.full(documentId ?? ''),
    queryFn: () => documentApi.details(documentId as string),
    staleTime: 30 * 1000,
    ...options,
    enabled: Boolean(documentId) && (options?.enabled ?? true),
  })
}

export function useDocumentTimeline(
  documentId: string | null | undefined,
  options?: EnabledOptions<DocumentTimelineEntryResponse[]>,
) {
  return useQuery({
    queryKey: documentKeys.timeline(documentId ?? ''),
    queryFn: () => documentApi.timeline(documentId as string),
    staleTime: 30 * 1000,
    ...options,
    enabled: Boolean(documentId) && (options?.enabled ?? true),
  })
}

/** Per-document access grants; requires `ACCESS_GRANT_MANAGE` on the server. */
export function useDocumentAccessGrants(
  documentId: string | null | undefined,
  options?: EnabledOptions<DocumentAccessGrantResponse[]>,
) {
  return useQuery({
    queryKey: documentKeys.grants(documentId ?? ''),
    queryFn: () => documentApi.accessGrants(documentId as string),
    staleTime: 30 * 1000,
    ...options,
    enabled: Boolean(documentId) && (options?.enabled ?? true),
  })
}
