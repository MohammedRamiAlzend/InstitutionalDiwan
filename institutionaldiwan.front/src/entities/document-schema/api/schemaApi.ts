import { httpClient } from '@/shared/api'
import type {
  DocumentFieldResponse,
  DocumentFieldsResponse,
  DocumentTypeSchemaResponse,
  FieldBehavior,
  SchemaFieldResponse,
  SchemaVersionResponse,
  SchemaVersionSummaryResponse,
} from '../model/types'
import { FIELD_OPTION_SOURCE } from '../model/types'

/** What an API without field behavior means: every field was offered as a filter, nothing else applied. */
const BEHAVIOR_OF_OLDER_API: FieldBehavior = {
  defaultValue: null,
  isReadOnly: false,
  isFilterable: true,
  showInTable: false,
  placeholderAr: null,
  helpTextAr: null,
  minLength: null,
  minDate: null,
  maxDate: null,
  visibleWhenFieldKey: null,
  visibleWhenValue: null,
  isHalfWidth: false,
  sectionTitleAr: null,
  optionSource: FIELD_OPTION_SOURCE.Static,
  lookupCatalog: null,
}

/** The SPA ships apart from the API, so a server not yet upgraded must not take the page down. */
function withBehavior(field: SchemaFieldResponse): SchemaFieldResponse {
  return field.behavior ? field : { ...field, behavior: BEHAVIOR_OF_OLDER_API }
}

function versionWithBehavior(version: SchemaVersionResponse): SchemaVersionResponse {
  return { ...version, fields: version.fields.map(withBehavior) }
}

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
    return versionWithBehavior(data)
  },

  /** `GET /api/document-types/{id}/schema` — the published schema; `schema` is null when none (`DOCUMENT_VIEW`). */
  async typeSchema(documentTypeId: number): Promise<DocumentTypeSchemaResponse> {
    const { data } = await httpClient.get<DocumentTypeSchemaResponse>(
      `/document-types/${documentTypeId}/schema`,
    )
    return { ...data, schema: data.schema && versionWithBehavior(data.schema) }
  },

  /** `GET /api/documents/{id}/fields` — the pinned schema with stored values (`DOCUMENT_VIEW` + VIEW access). */
  async documentFields(documentId: string): Promise<DocumentFieldsResponse> {
    const { data } = await httpClient.get<DocumentFieldsResponse>(`/documents/${documentId}/fields`)
    return {
      ...data,
      fields: data.fields.map((entry) => ({ ...entry, field: withBehavior(entry.field) })),
    }
  },
}
