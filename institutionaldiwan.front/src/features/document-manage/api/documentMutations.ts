import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  documentKeys,
  type DocumentParticipantResponse,
  type DocumentResponse,
} from '@/entities/document'
import { httpClient } from '@/shared/api'
import type {
  AddDocumentParticipantRequest,
  CancelDocumentRequest,
  RegisterDraftDocumentRequest,
  UpdateDraftDocumentRequest,
} from '../model/types'

/** Shared invalidation for commands that change one document (and its timeline). */
function useDocumentInvalidation(documentId: string) {
  const queryClient = useQueryClient()

  return () => {
    void queryClient.invalidateQueries({ queryKey: documentKeys.detail(documentId) })
    void queryClient.invalidateQueries({ queryKey: documentKeys.lists() })
  }
}

/** `PUT /api/documents/{documentId}` — rejected by the server once the draft is registered. */
export function useUpdateDraftDocumentMutation(documentId: string) {
  const invalidate = useDocumentInvalidation(documentId)

  return useMutation({
    mutationFn: async (
      request: UpdateDraftDocumentRequest,
    ): Promise<DocumentResponse> => {
      const { data } = await httpClient.put<DocumentResponse>(
        `/documents/${documentId}`,
        request,
      )
      return data
    },
    onSuccess: invalidate,
  })
}

/** `POST /api/documents/{documentId}/register` — allocates the next official number. */
export function useRegisterDraftDocumentMutation(documentId: string) {
  const invalidate = useDocumentInvalidation(documentId)

  return useMutation({
    mutationFn: async (
      request: RegisterDraftDocumentRequest,
    ): Promise<DocumentResponse> => {
      const { data } = await httpClient.post<DocumentResponse>(
        `/documents/${documentId}/register`,
        request,
      )
      return data
    },
    onSuccess: invalidate,
  })
}

/** `POST /api/documents/{documentId}/participants` — one external party per document. */
export function useAddDocumentParticipantMutation(documentId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (
      request: AddDocumentParticipantRequest,
    ): Promise<DocumentParticipantResponse> => {
      const { data } = await httpClient.post<DocumentParticipantResponse>(
        `/documents/${documentId}/participants`,
        request,
      )
      return data
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: documentKeys.detail(documentId) })
    },
  })
}

/** `POST /api/documents/{documentId}/cancel` — blocked while movements are active. */
export function useCancelDocumentMutation(documentId: string) {
  const invalidate = useDocumentInvalidation(documentId)

  return useMutation({
    mutationFn: async (request: CancelDocumentRequest): Promise<DocumentResponse> => {
      const { data } = await httpClient.post<DocumentResponse>(
        `/documents/${documentId}/cancel`,
        request,
      )
      return data
    },
    onSuccess: invalidate,
  })
}
