import { useMutation, useQueryClient } from '@tanstack/react-query'
import { documentKeys, type DocumentAccessGrantResponse } from '@/entities/document'
import { httpClient } from '@/shared/api'
import type { GrantDocumentAccessRequest } from '../model/types'

/** Both commands only change the per-document grant list. */
function useGrantInvalidation(documentId: string) {
  const queryClient = useQueryClient()

  return () => {
    void queryClient.invalidateQueries({ queryKey: documentKeys.grants(documentId) })
  }
}

/** `POST /api/documents/{documentId}/access-grants` — records the caller as grantor. */
export function useGrantDocumentAccessMutation(documentId: string) {
  const invalidate = useGrantInvalidation(documentId)

  return useMutation({
    mutationFn: async (
      request: GrantDocumentAccessRequest,
    ): Promise<DocumentAccessGrantResponse> => {
      const { data } = await httpClient.post<DocumentAccessGrantResponse>(
        `/documents/${documentId}/access-grants`,
        request,
      )
      return data
    },
    onSuccess: invalidate,
  })
}

/** `DELETE /api/documents/{documentId}/access-grants/{grantId}` — revoke only, never delete. */
export function useRevokeDocumentAccessMutation(documentId: string) {
  const invalidate = useGrantInvalidation(documentId)

  return useMutation({
    mutationFn: async (grantId: string): Promise<DocumentAccessGrantResponse> => {
      const { data } = await httpClient.delete<DocumentAccessGrantResponse>(
        `/documents/${documentId}/access-grants/${grantId}`,
      )
      return data
    },
    onSuccess: invalidate,
  })
}
