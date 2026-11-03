import { httpClient } from '@/shared/api'
import type {
  DocumentFieldResponse,
  DocumentFieldsResponse,
  DocumentTypeSchemaResponse,
  SchemaVersionResponse,
  SchemaVersionSummaryResponse,
} from '../model/types'

export const schemaApi = {
  /** `GET /api/document-types/{id}/fields` — `LOOKUP_MANAGE`. */
  async fields(documentTypeId: number): Promise<DocumentFieldResponse[]> {
    const { data } = await httpClient.get<DocumentFieldResponse[]>(
      `/document-types/${documentTypeId}/fields`,
    )
    return data
  },

  /** `GET /api/document-types/{id}/schema-versions` — history, newest first (`LOOKUP_MANAGE`). */
  async versions(documentTypeId: number): Promise<SchemaVersionSummaryResponse[]> {
    const { data } = await httpClient.get<SchemaVersionSummaryResponse[]>(
      `/document-types/${documentTypeId}/schema-versions`,
    )
    return data
  },

  /** `GET /api/schema-versions/{id}` — one version with its fields (`LOOKUP_MANAGE`). */
  async version(schemaVersionId: string): Promise<SchemaVersionResponse> {
    const { data } = await httpClient.get<SchemaVersionResponse>(
      `/schema-versions/${schemaVersionId}`,
    )
    return data
  },

  /** `GET /api/document-types/{id}/schema` — the published schema; `schema` is null when none (`DOCUMENT_VIEW`). */
  async typeSchema(documentTypeId: number): Promise<DocumentTypeSchemaResponse> {
    const { data } = await httpClient.get<DocumentTypeSchemaResponse>(
      `/document-types/${documentTypeId}/schema`,
    )
    return data
  },

  /** `GET /api/documents/{id}/fields` — the pinned schema with stored values (`DOCUMENT_VIEW` + VIEW access). */
  async documentFields(documentId: string): Promise<DocumentFieldsResponse> {
    const { data } = await httpClient.get<DocumentFieldsResponse>(`/documents/${documentId}/fields`)
    return data
  },
}
