import { useMutation, useQueryClient } from '@tanstack/react-query'
import { documentKeys, type DocumentResponse } from '@/entities/document'
import { httpClient } from '@/shared/api'
import type { CreateDocumentRequest, CreateDraftDocumentRequest } from '../model/types'

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
