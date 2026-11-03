import { useMutation, useQueryClient } from '@tanstack/react-query'
import { relationKeys, type DocumentRelationResponse } from '@/entities/document-relation'
import { httpClient } from '@/shared/api'
import type { CreateDocumentRelationRequest, RemoveDocumentRelationRequest } from '../model/types'

/**
 * A link is visible from both documents, so every cached relation read (lists, graphs and
 * histories of any document) is refetched rather than only the route document's.
 */
function useRelationInvalidation() {
  const queryClient = useQueryClient()
  return () => queryClient.invalidateQueries({ queryKey: relationKeys.all })
}

/** `POST /api/documents/{documentId}/relations` — needs `DOCUMENT_EDIT`; the first document must not be archived. */
export function useCreateRelationMutation(documentId: string) {
  const invalidate = useRelationInvalidation()

  return useMutation({
    mutationFn: async (request: CreateDocumentRelationRequest): Promise<DocumentRelationResponse> => {
      const { data } = await httpClient.post<DocumentRelationResponse>(
        `/documents/${documentId}/relations`,
        request,
      )
      return data
    },
    onSuccess: invalidate,
  })
}

/** `DELETE /api/documents/{documentId}/relations/{relationId}` — the reason travels in the JSON body. */
export function useRemoveRelationMutation(documentId: string) {
  const invalidate = useRelationInvalidation()

  return useMutation({
    mutationFn: async ({
      relationId,
      reason,
    }: RemoveDocumentRelationRequest & { relationId: string }): Promise<void> => {
      await httpClient.delete(`/documents/${documentId}/relations/${relationId}`, {
        data: { reason },
      })
    },
    onSuccess: invalidate,
  })
}
