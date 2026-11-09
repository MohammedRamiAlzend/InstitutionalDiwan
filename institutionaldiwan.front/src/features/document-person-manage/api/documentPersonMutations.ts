import { useMutation, useQueryClient } from '@tanstack/react-query'
import { personKeys, type DocumentPersonResponse } from '@/entities/person'
import { httpClient } from '@/shared/api'

export interface AddDocumentPersonRequest {
  personId?: string
  fullName?: string
  roleId?: number
}

/** `POST /api/documents/{id}/persons` — names an existing or new person on the document. */
export function useAddDocumentPersonMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      documentId,
      request,
    }: {
      documentId: string
      request: AddDocumentPersonRequest
    }): Promise<DocumentPersonResponse> => {
      const { data } = await httpClient.post<DocumentPersonResponse>(
        `/documents/${documentId}/persons`,
        request,
      )
      return data
    },
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: personKeys.all })
    },
  })
}

/** `PUT /api/documents/{id}/persons/{documentPersonId}` — changes the person's role. */
export function useChangeDocumentPersonRoleMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      documentId,
      documentPersonId,
      roleId,
    }: {
      documentId: string
      documentPersonId: string
      roleId: number
    }): Promise<DocumentPersonResponse> => {
      const { data } = await httpClient.put<DocumentPersonResponse>(
        `/documents/${documentId}/persons/${documentPersonId}`,
        { roleId },
      )
      return data
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: personKeys.all })
    },
  })
}

/** `DELETE /api/documents/{id}/persons/{documentPersonId}` — takes the person off the document. */
export function useRemoveDocumentPersonMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      documentId,
      documentPersonId,
    }: {
      documentId: string
      documentPersonId: string
    }): Promise<void> => {
      await httpClient.delete(`/documents/${documentId}/persons/${documentPersonId}`)
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: personKeys.all })
    },
  })
}
