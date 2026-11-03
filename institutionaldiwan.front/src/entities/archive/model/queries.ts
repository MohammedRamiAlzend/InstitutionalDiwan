import { useQuery, type UseQueryOptions } from '@tanstack/react-query'
import type { PagedResult } from '@/shared/types'
import { archiveApi } from '../api/archiveApi'
import type {
  ArchiveSearchParams,
  ArchivedDocumentResponse,
  DocumentArchiveEventResponse,
  SavedSearchResponse,
} from './types'

export const archiveKeys = {
  all: ['archive'] as const,
  searches: () => [...archiveKeys.all, 'search'] as const,
  search: (params: ArchiveSearchParams) => [...archiveKeys.searches(), params] as const,
  events: (documentId: string) => [...archiveKeys.all, 'events', documentId] as const,
  savedSearches: () => [...archiveKeys.all, 'saved-searches'] as const,
}

type EnabledOptions<T> = Partial<Pick<UseQueryOptions<T>, 'enabled'>>

/** Access-filtered archive grid; counters follow the same rule as the rows. */
export function useArchivedDocuments(
  params: ArchiveSearchParams,
  options?: EnabledOptions<PagedResult<ArchivedDocumentResponse>>,
) {
  return useQuery({
    queryKey: archiveKeys.search(params),
    queryFn: () => archiveApi.search(params),
    placeholderData: (previous) => previous,
    staleTime: 30 * 1000,
    ...options,
  })
}

/** A document's archive history; empty for a document that was never archived. */
export function useDocumentArchiveEvents(documentId: string | undefined) {
  return useQuery({
    queryKey: archiveKeys.events(documentId ?? ''),
    queryFn: (): Promise<DocumentArchiveEventResponse[]> => archiveApi.events(documentId ?? ''),
    enabled: Boolean(documentId),
  })
}

/** The caller's saved searches (private to them; at most 50). */
export function useSavedSearches(options?: EnabledOptions<SavedSearchResponse[]>) {
  return useQuery({
    queryKey: archiveKeys.savedSearches(),
    queryFn: () => archiveApi.savedSearches(),
    staleTime: 60 * 1000,
    ...options,
  })
}
