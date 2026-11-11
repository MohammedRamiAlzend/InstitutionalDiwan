import { httpClient } from '@/shared/api'
import type {
  DepartmentRegisterFormsResponse,
  DocumentDepartmentRecordsResponse,
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

/** Query string of a form: nothing for the creation form, `departmentId` for a department record form. */
export function formParams(departmentId?: string | null): { departmentId?: string } {
  return departmentId ? { departmentId } : {}
}

export const schemaApi = {
  /** `GET /api/document-types/{id}/fields?departmentId=` — one form's fields (`LOOKUP_MANAGE`). */
  async fields(documentTypeId: number, departmentId?: string | null): Promise<DocumentFieldResponse[]> {
    const { data } = await httpClient.get<DocumentFieldResponse[]>(
      `/document-types/${documentTypeId}/fields`,
      { params: formParams(departmentId) },
    )
    return data
  },

  /** `GET /api/document-types/{id}/schema-versions?departmentId=` — one form's history, newest first (`LOOKUP_MANAGE`). */
  async versions(
    documentTypeId: number,
    departmentId?: string | null,
  ): Promise<SchemaVersionSummaryResponse[]> {
    const { data } = await httpClient.get<SchemaVersionSummaryResponse[]>(
      `/document-types/${documentTypeId}/schema-versions`,
      { params: formParams(departmentId) },
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

  /**
   * `GET /api/document-types/{id}/schema?departmentId=` — a form's published schema (the creation
   * form, or a department's record form); `schema` is null when none (`DOCUMENT_VIEW`).
   */
  async typeSchema(
    documentTypeId: number,
    departmentId?: string | null,
  ): Promise<DocumentTypeSchemaResponse> {
    const { data } = await httpClient.get<DocumentTypeSchemaResponse>(
      `/document-types/${documentTypeId}/schema`,
      { params: formParams(departmentId) },
    )
    return { ...data, schema: data.schema && versionWithBehavior(data.schema) }
  },

  /**
   * `GET /api/documents/{id}/department-records` — each department's register line with the record
   * of every visit, filtered by the server to what the caller may read (`DOCUMENT_VIEW`).
   */
  async departmentRecords(documentId: string): Promise<DocumentDepartmentRecordsResponse> {
    const { data } = await httpClient.get<DocumentDepartmentRecordsResponse>(
      `/documents/${documentId}/department-records`,
    )
    return {
      ...data,
      records: data.records.map((record) => ({
        ...record,
        visits: record.visits.map((visit) => ({
          ...visit,
          fields: visit.fields.map((entry) => ({ ...entry, field: withBehavior(entry.field) })),
        })),
      })),
    }
  },

  /**
   * `GET /api/departments/{id}/register/forms` — every published or retired record-form version of a
   * department, per document type and newest first (`NUMBERING_VIEW`).
   */
  async departmentRegisterForms(departmentId: string): Promise<DepartmentRegisterFormsResponse> {
    const { data } = await httpClient.get<DepartmentRegisterFormsResponse>(
      `/departments/${departmentId}/register/forms`,
    )
    return {
      forms: data.forms.map((form) => ({
        ...form,
        versions: form.versions.map(versionWithBehavior),
      })),
    }
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
