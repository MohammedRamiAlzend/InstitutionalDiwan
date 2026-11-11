import { useMutation, useQueryClient } from '@tanstack/react-query'
import { documentDecisionKeys, type DocumentDecisionResponse } from '@/entities/document-decision'
import { documentKeys } from '@/entities/document'
import { httpClient } from '@/shared/api'

function useInvalidateDecisions(documentId: string) {
  const queryClient = useQueryClient()
  return () => {
    void queryClient.invalidateQueries({ queryKey: documentDecisionKeys.all })
    void queryClient.invalidateQueries({ queryKey: documentKeys.detail(documentId) })
  }
}

export interface IssueDecisionRequest {
  decisionTypeId: string
  decisionNumber: string
  decisionDate: string
  summary: string | null
  documentFileId: string | null
}

export function useIssueDecisionMutation(documentId: string) {
  const invalidate = useInvalidateDecisions(documentId)
  return useMutation({
    mutationFn: async (request: IssueDecisionRequest): Promise<DocumentDecisionResponse> => {
      const { data } = await httpClient.post<DocumentDecisionResponse>(
        `/documents/${documentId}/decisions`,
        request,
      )
      return data
    },
    onSuccess: invalidate,
  })
}

export function useAttachDecisionFileMutation(documentId: string) {
  const invalidate = useInvalidateDecisions(documentId)
  return useMutation({
    mutationFn: async (input: {
      decisionId: string
      documentFileId: string
    }): Promise<DocumentDecisionResponse> => {
      const { data } = await httpClient.put<DocumentDecisionResponse>(
        `/decisions/${input.decisionId}/file`,
        { documentFileId: input.documentFileId },
      )
      return data
    },
    onSuccess: invalidate,
  })
}

export function useCancelDecisionMutation(documentId: string) {
  const invalidate = useInvalidateDecisions(documentId)
  return useMutation({
    mutationFn: async (input: { decisionId: string; reason: string }): Promise<DocumentDecisionResponse> => {
      const { data } = await httpClient.post<DocumentDecisionResponse>(
        `/decisions/${input.decisionId}/cancel`,
        { reason: input.reason },
      )
      return data
    },
    onSuccess: invalidate,
  })
}
