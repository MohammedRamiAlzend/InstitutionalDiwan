import { useMutation, useQueryClient } from '@tanstack/react-query'
import { archiveKeys, type SavedSearchResponse } from '@/entities/archive'
import { documentKeys, type DocumentResponse } from '@/entities/document'
import { httpClient } from '@/shared/api'
import type {
  ArchiveDocumentRequest,
  SaveSearchRequest,
  UnarchiveDocumentRequest,
} from '../model/types'

/**
 * Archiving changes the document's status, so its details, the registry lists and the archive grid
 * must all refetch; the archive history of that document changes too.
 */
function useArchiveTransitionInvalidation(documentId: string) {
  const queryClient = useQueryClient()

  return () => {
    void queryClient.invalidateQueries({ queryKey: documentKeys.detail(documentId) })
    void queryClient.invalidateQueries({ queryKey: documentKeys.lists() })
    void queryClient.invalidateQueries({ queryKey: archiveKeys.searches() })
    void queryClient.invalidateQueries({ queryKey: archiveKeys.events(documentId) })
  }
}

/** `POST /api/documents/{documentId}/archive` — registered documents only; needs `MANAGE` access. */
export function useArchiveDocumentMutation(documentId: string) {
  const invalidate = useArchiveTransitionInvalidation(documentId)

  return useMutation({
    mutationFn: async (request: ArchiveDocumentRequest): Promise<DocumentResponse> => {
      const { data } = await httpClient.post<DocumentResponse>(
        `/documents/${documentId}/archive`,
        request,
      )
      return data
    },
    onSuccess: invalidate,
  })
}

/** `POST /api/documents/{documentId}/unarchive` — restores the status held before archiving. */
export function useUnarchiveDocumentMutation(documentId: string) {
  const invalidate = useArchiveTransitionInvalidation(documentId)

  return useMutation({
    mutationFn: async (request: UnarchiveDocumentRequest): Promise<DocumentResponse> => {
      const { data } = await httpClient.post<DocumentResponse>(
        `/documents/${documentId}/unarchive`,
        request,
      )
      return data
    },
    onSuccess: invalidate,
  })
}

/** `POST /api/saved-searches` — at most 50 per user, one default. */
export function useCreateSavedSearchMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (request: SaveSearchRequest): Promise<SavedSearchResponse> => {
      const { data } = await httpClient.post<SavedSearchResponse>('/saved-searches', request)
      return data
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: archiveKeys.savedSearches() })
    },
  })
}

/** `PUT /api/saved-searches/{savedSearchId}` — replaces name, criteria and default flag. */
export function useUpdateSavedSearchMutation(savedSearchId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (request: SaveSearchRequest): Promise<SavedSearchResponse> => {
      const { data } = await httpClient.put<SavedSearchResponse>(
        `/saved-searches/${savedSearchId}`,
        request,
      )
      return data
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: archiveKeys.savedSearches() })
    },
  })
}

/** `DELETE /api/saved-searches/{savedSearchId}` — permanent, owner only. */
export function useDeleteSavedSearchMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (savedSearchId: string): Promise<void> => {
      await httpClient.delete(`/saved-searches/${savedSearchId}`)
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: archiveKeys.savedSearches() })
    },
  })
}
