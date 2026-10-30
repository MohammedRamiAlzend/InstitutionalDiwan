import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  documentKeys,
  type DocumentParticipantResponse,
  type DocumentResponse,
} from '@/entities/document'
import { httpClient } from '@/shared/api'
import type {
  CreateDocumentRecipientRequest,
  CreateDocumentRequest,
  CreateDraftDocumentRequest,
} from '../model/types'

/** `POST /api/documents` — registered immediately with the supplied official number. */
export function useCreateDocumentMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (request: CreateDocumentRequest): Promise<DocumentResponse> => {
      const { data } = await httpClient.post<DocumentResponse>('/documents', request)
      return data
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: documentKeys.lists() })
    },
  })
}

/** `POST /api/documents/drafts` — saved as DRAFT until registered. */
export function useCreateDraftDocumentMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (request: CreateDraftDocumentRequest): Promise<DocumentResponse> => {
      const { data } = await httpClient.post<DocumentResponse>('/documents/drafts', request)
      return data
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: documentKeys.lists() })
    },
  })
}

/** `POST /api/documents/{documentId}/participants` — attaches the optional recipient after creation. */
export function useAddCreationRecipientMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      documentId,
      request,
    }: {
      documentId: string
      request: CreateDocumentRecipientRequest
    }): Promise<DocumentParticipantResponse> => {
      const { data } = await httpClient.post<DocumentParticipantResponse>(
        `/documents/${documentId}/participants`,
        request,
      )
      return data
    },
    onSuccess: (_participant, { documentId }) => {
      void queryClient.invalidateQueries({ queryKey: documentKeys.detail(documentId) })
      void queryClient.invalidateQueries({ queryKey: documentKeys.lists() })
    },
  })
}
