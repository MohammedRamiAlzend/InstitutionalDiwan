import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  schemaKeys,
  type DocumentFieldsResponse,
  type FieldValueRequest,
} from '@/entities/document-schema'
import { httpClient } from '@/shared/api'

/**
 * `PUT /api/documents/{documentId}/fields` — replaces every value of a draft (`DOCUMENT_EDIT` plus
 * DOWNLOAD access). A blank value clears the field; registered documents answer a conflict.
 */
export function useSetDocumentFieldValuesMutation(documentId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (values: FieldValueRequest[]): Promise<DocumentFieldsResponse> => {
      const { data } = await httpClient.put<DocumentFieldsResponse>(
        `/documents/${documentId}/fields`,
        { values },
      )
      return data
    },
    onSuccess: (data) => {
      queryClient.setQueryData(schemaKeys.documentFields(documentId), data)
    },
  })
}
