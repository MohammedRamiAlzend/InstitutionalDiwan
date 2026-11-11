import { useMutation, useQueryClient } from '@tanstack/react-query'
import { documentDecisionKeys, type DocumentDecisionResponse } from '@/entities/document-decision'
import { documentKeys } from '@/entities/document'
import { relationKeys } from '@/entities/document-relation'
import { httpClient } from '@/shared/api'

function useInvalidateDecisions(documentId: string, alsoRelations = false) {
  const queryClient = useQueryClient()
  return () => {
    void queryClient.invalidateQueries({ queryKey: documentDecisionKeys.all })
    void queryClient.invalidateQueries({ queryKey: documentKeys.detail(documentId) })
    if (alsoRelations) {
      void queryClient.invalidateQueries({ queryKey: relationKeys.all })
    }
  }
}

export interface IssueDecisionRequest {
  decisionTypeId: string
  decisionNumber: string
  decisionDate: string
  summary: string | null
  documentFileId: string | null
  joinTargetDocumentId: string | null
}

export function useIssueDecisionMutation(documentId: string) {
  const invalidate = useInvalidateDecisions(documentId, true)
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

export function useRecordAppealMutation(documentId: string) {
  const invalidate = useInvalidateDecisions(documentId)
  return useMutation({
    mutationFn: async (input: {
      decisionId: string
      appealDate: string
      appellantDepartmentId: string
      referenceNumber: string | null
      notes: string | null
    }): Promise<DocumentDecisionResponse> => {
      const { data } = await httpClient.post<DocumentDecisionResponse>(
        `/decisions/${input.decisionId}/appeals`,
        {
          appealDate: input.appealDate,
          appellantDepartmentId: input.appellantDepartmentId,
          referenceNumber: input.referenceNumber,
          notes: input.notes,
        },
      )
      return data
    },
    onSuccess: invalidate,
  })
}

export function useRecordAppealOutcomeMutation(documentId: string) {
  const invalidate = useInvalidateDecisions(documentId)
  return useMutation({
    mutationFn: async (input: {
      decisionId: string
      appealId: string
      outcomeId: number
      outcomeDate: string
    }): Promise<DocumentDecisionResponse> => {
      const { data } = await httpClient.put<DocumentDecisionResponse>(
        `/decisions/${input.decisionId}/appeals/${input.appealId}/outcome`,
        { outcomeId: input.outcomeId, outcomeDate: input.outcomeDate },
      )
      return data
    },
    onSuccess: invalidate,
  })
}

export function useCancelDecisionMutation(documentId: string) {
  const invalidate = useInvalidateDecisions(documentId, true)
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
