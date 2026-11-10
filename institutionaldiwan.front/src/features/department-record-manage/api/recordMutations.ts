import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  schemaKeys,
  type DepartmentRecordVisitResponse,
  type FieldValueRequest,
} from '@/entities/document-schema'
import { httpClient } from '@/shared/api'

/** A record write changes only that document's department records. */
function useRecordInvalidation(documentId: string) {
  const queryClient = useQueryClient()
  return () => queryClient.invalidateQueries({ queryKey: schemaKeys.departmentRecords(documentId) })
}

/**
 * `PUT /api/registry-visits/{visitId}/fields` — replaces the values of an open record. Only the
 * department that keeps it may edit (`DOCUMENT_EDIT` and its scope); a blank value clears a field.
 */
export function useSetVisitRecordMutation(documentId: string) {
  const invalidate = useRecordInvalidation(documentId)
  return useMutation({
    mutationFn: async ({
      visitId,
      values,
    }: {
      visitId: string
      values: FieldValueRequest[]
    }): Promise<DepartmentRecordVisitResponse> => {
      const { data } = await httpClient.put<DepartmentRecordVisitResponse>(
        `/registry-visits/${visitId}/fields`,
        { values },
      )
      return data
    },
    onSuccess: invalidate,
  })
}

/** `PUT /api/registry-visits/{visitId}/fields/correct` — corrects a locked record with a reason (`DEPARTMENT_RECORD_CORRECT`). */
export function useCorrectVisitRecordMutation(documentId: string) {
  const invalidate = useRecordInvalidation(documentId)
  return useMutation({
    mutationFn: async ({
      visitId,
      values,
      reason,
    }: {
      visitId: string
      values: FieldValueRequest[]
      reason: string
    }): Promise<DepartmentRecordVisitResponse> => {
      const { data } = await httpClient.put<DepartmentRecordVisitResponse>(
        `/registry-visits/${visitId}/fields/correct`,
        { values, reason },
      )
      return data
    },
    onSuccess: invalidate,
  })
}
