import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  schemaKeys,
  type DocumentFieldResponse,
  type SchemaVersionResponse,
} from '@/entities/document-schema'
import { httpClient } from '@/shared/api'
import type { AddSchemaFieldRequest, CreateFieldRequest, FieldSettingsRequest } from '../model/types'

/** Every schema write refetches the type's reads, the single version and the published form schema. */
function useSchemaInvalidation() {
  const queryClient = useQueryClient()
  return () => queryClient.invalidateQueries({ queryKey: schemaKeys.all })
}

/** `POST /api/document-types/{id}/fields` — defines a field identity (`LOOKUP_MANAGE`). */
export function useCreateFieldMutation(documentTypeId: number) {
  const invalidate = useSchemaInvalidation()
  return useMutation({
    mutationFn: async (request: CreateFieldRequest): Promise<DocumentFieldResponse> => {
      const { data } = await httpClient.post<DocumentFieldResponse>(
        `/document-types/${documentTypeId}/fields`,
        request,
      )
      return data
    },
    onSuccess: invalidate,
  })
}

/** `POST /api/document-types/{id}/schema-versions` — starts the single draft, copied from the published version. */
export function useCreateDraftMutation(documentTypeId: number) {
  const invalidate = useSchemaInvalidation()
  return useMutation({
    mutationFn: async (): Promise<SchemaVersionResponse> => {
      const { data } = await httpClient.post<SchemaVersionResponse>(
        `/document-types/${documentTypeId}/schema-versions`,
      )
      return data
    },
    onSuccess: invalidate,
  })
}

/** `DELETE /api/schema-versions/{id}` — draft only. */
export function useDeleteDraftMutation() {
  const invalidate = useSchemaInvalidation()
  return useMutation({
    mutationFn: async (schemaVersionId: string): Promise<void> => {
      await httpClient.delete(`/schema-versions/${schemaVersionId}`)
    },
    onSuccess: invalidate,
  })
}

/** `POST /api/schema-versions/{id}/fields` — adds a field to the draft. */
export function useAddVersionFieldMutation(schemaVersionId: string) {
  const invalidate = useSchemaInvalidation()
  return useMutation({
    mutationFn: async (request: AddSchemaFieldRequest): Promise<SchemaVersionResponse> => {
      const { data } = await httpClient.post<SchemaVersionResponse>(
        `/schema-versions/${schemaVersionId}/fields`,
        request,
      )
      return data
    },
    onSuccess: invalidate,
  })
}

/** `PUT /api/schema-versions/{id}/fields/{fieldId}` — replaces a draft field's settings. */
export function useUpdateVersionFieldMutation(schemaVersionId: string) {
  const invalidate = useSchemaInvalidation()
  return useMutation({
    mutationFn: async ({
      documentFieldId,
      settings,
    }: {
      documentFieldId: string
      settings: FieldSettingsRequest
    }): Promise<SchemaVersionResponse> => {
      const { data } = await httpClient.put<SchemaVersionResponse>(
        `/schema-versions/${schemaVersionId}/fields/${documentFieldId}`,
        settings,
      )
      return data
    },
    onSuccess: invalidate,
  })
}

/** `DELETE /api/schema-versions/{id}/fields/{fieldId}` — removes a field from the draft only. */
export function useRemoveVersionFieldMutation(schemaVersionId: string) {
  const invalidate = useSchemaInvalidation()
  return useMutation({
    mutationFn: async (documentFieldId: string): Promise<SchemaVersionResponse> => {
      const { data } = await httpClient.delete<SchemaVersionResponse>(
        `/schema-versions/${schemaVersionId}/fields/${documentFieldId}`,
      )
      return data
    },
    onSuccess: invalidate,
  })
}

/** `POST /api/schema-versions/{id}/publish` — freezes the draft and retires the previous version. */
export function usePublishVersionMutation() {
  const invalidate = useSchemaInvalidation()
  return useMutation({
    mutationFn: async (schemaVersionId: string): Promise<SchemaVersionResponse> => {
      const { data } = await httpClient.post<SchemaVersionResponse>(
        `/schema-versions/${schemaVersionId}/publish`,
      )
      return data
    },
    onSuccess: invalidate,
  })
}
