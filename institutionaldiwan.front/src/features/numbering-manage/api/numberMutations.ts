import { useMutation, useQueryClient } from '@tanstack/react-query'
import { documentKeys, type DocumentNumberResponse } from '@/entities/document'
import { numberingRegistryKeys } from '@/entities/numbering-registry'
import { httpClient } from '@/shared/api'
import type {
  CancelOfficialNumberRequest,
  IssueOfficialNumberRequest,
  RecordDepartmentRegistrationRequest,
  ReplaceOfficialNumberRequest,
} from '../model/types'

/** Numbers change the document, its number reads, the registry lists, and counters. */
function useNumberInvalidation(documentId: string) {
  const queryClient = useQueryClient()

  return () => {
    void queryClient.invalidateQueries({ queryKey: documentKeys.detail(documentId) })
    void queryClient.invalidateQueries({ queryKey: documentKeys.lists() })
    void queryClient.invalidateQueries({ queryKey: numberingRegistryKeys.all })
  }
}

/** `POST /api/documents/{documentId}/numbers/issue` — draft documents only. */
export function useIssueOfficialNumberMutation(documentId: string) {
  const invalidate = useNumberInvalidation(documentId)

  return useMutation({
    mutationFn: async (
      request: IssueOfficialNumberRequest,
    ): Promise<DocumentNumberResponse> => {
      const { data } = await httpClient.post<DocumentNumberResponse>(
        `/documents/${documentId}/numbers/issue`,
        request,
      )
      return data
    },
    onSuccess: invalidate,
  })
}

/** `POST /api/documents/{documentId}/numbers/{documentNumberId}/cancel`. */
export function useCancelOfficialNumberMutation(documentId: string) {
  const invalidate = useNumberInvalidation(documentId)

  return useMutation({
    mutationFn: async ({
      documentNumberId,
      ...request
    }: CancelOfficialNumberRequest & { documentNumberId: string }) => {
      const { data } = await httpClient.post<DocumentNumberResponse>(
        `/documents/${documentId}/numbers/${documentNumberId}/cancel`,
        request,
      )
      return data
    },
    onSuccess: invalidate,
  })
}

/** `POST /api/documents/{documentId}/numbers/{documentNumberId}/replace`. */
export function useReplaceOfficialNumberMutation(documentId: string) {
  const invalidate = useNumberInvalidation(documentId)

  return useMutation({
    mutationFn: async ({
      documentNumberId,
      ...request
    }: ReplaceOfficialNumberRequest & { documentNumberId: string }) => {
      const { data } = await httpClient.post<DocumentNumberResponse>(
        `/documents/${documentId}/numbers/${documentNumberId}/replace`,
        request,
      )
      return data
    },
    onSuccess: invalidate,
  })
}

/** `POST /api/documents/{documentId}/numbers/department-registration`. */
export function useRecordDepartmentRegistrationMutation(documentId: string) {
  const invalidate = useNumberInvalidation(documentId)

  return useMutation({
    mutationFn: async (
      request: RecordDepartmentRegistrationRequest,
    ): Promise<DocumentNumberResponse> => {
      const { data } = await httpClient.post<DocumentNumberResponse>(
        `/documents/${documentId}/numbers/department-registration`,
        request,
      )
      return data
    },
    onSuccess: invalidate,
  })
}
