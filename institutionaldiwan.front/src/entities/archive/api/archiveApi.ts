import { httpClient } from '@/shared/api'
import type { PagedResult } from '@/shared/types'
import type {
  ArchiveSearchParams,
  ArchivedDocumentResponse,
  DocumentArchiveEventResponse,
  SavedSearchResponse,
} from '../model/types'

export const archiveApi = {
  /** `GET /api/archive/documents` — only archived documents the caller may view. */
  async search(params: ArchiveSearchParams): Promise<PagedResult<ArchivedDocumentResponse>> {
    const { data } = await httpClient.get<PagedResult<ArchivedDocumentResponse>>(
      '/archive/documents',
      { params },
    )
    return data
  },

  /** `GET /api/documents/{documentId}/archive-events` — newest first. */
  async events(documentId: string): Promise<DocumentArchiveEventResponse[]> {
    const { data } = await httpClient.get<DocumentArchiveEventResponse[]>(
      `/documents/${documentId}/archive-events`,
    )
    return data
  },

  /** `GET /api/saved-searches` — the caller's own searches, ordered by name. */
  async savedSearches(): Promise<SavedSearchResponse[]> {
    const { data } = await httpClient.get<SavedSearchResponse[]>('/saved-searches')
    return data
  },
}
