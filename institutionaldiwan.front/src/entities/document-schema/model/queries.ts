import { useQuery, type UseQueryOptions } from '@tanstack/react-query'
import { schemaApi } from '../api/schemaApi'
import type {
  DocumentDepartmentRecordsResponse,
  DocumentFieldResponse,
  DocumentFieldsResponse,
  DocumentTypeSchemaResponse,
  SchemaVersionResponse,
  SchemaVersionSummaryResponse,
} from './types'

/** Query-key segment of a form: the creation form, or one department's record form. */
const formKey = (departmentId?: string | null) => departmentId ?? 'creation'

export const schemaKeys = {
  all: ['document-schema'] as const,
  fields: (documentTypeId: number, departmentId?: string | null) =>
    [...schemaKeys.all, 'fields', documentTypeId, formKey(departmentId)] as const,
  versions: (documentTypeId: number, departmentId?: string | null) =>
    [...schemaKeys.all, 'versions', documentTypeId, formKey(departmentId)] as const,
  version: (schemaVersionId: string) => [...schemaKeys.all, 'version', schemaVersionId] as const,
  typeSchema: (documentTypeId: number, departmentId?: string | null) =>
    [...schemaKeys.all, 'type-schema', documentTypeId, formKey(departmentId)] as const,
  documentFields: (documentId: string) => [...schemaKeys.all, 'document', documentId] as const,
  departmentRecordsAll: ['document-schema', 'department-records'] as const,
  departmentRecords: (documentId: string) =>
    [...schemaKeys.departmentRecordsAll, documentId] as const,
}

/** The department records on a document that the caller may read (each visit's record form values). */
export function useDepartmentRecords(
  documentId: string | undefined,
  options?: Partial<Pick<UseQueryOptions<DocumentDepartmentRecordsResponse>, 'enabled'>>,
) {
  return useQuery({
    queryKey: schemaKeys.departmentRecords(documentId ?? ''),
    queryFn: () => schemaApi.departmentRecords(documentId ?? ''),
    enabled: Boolean(documentId) && (options?.enabled ?? true),
  })
}

/**
 * `departmentId` picks a department's record form; without it the hooks read the creation form,
 * exactly as before department forms existed.
 */
type FormOptions<T> = Partial<Pick<UseQueryOptions<T>, 'enabled'>> & { departmentId?: string | null }

/** The fields defined for one form of a document type (administration). */
export function useDocumentTypeFields(
  documentTypeId: number | undefined,
  options?: FormOptions<DocumentFieldResponse[]>,
) {
  const departmentId = options?.departmentId ?? null
  return useQuery({
    queryKey: schemaKeys.fields(documentTypeId ?? 0, departmentId),
    queryFn: () => schemaApi.fields(documentTypeId ?? 0, departmentId),
    enabled: documentTypeId !== undefined && (options?.enabled ?? true),
  })
}

/** The schema version history of one form of a document type, newest first (administration). */
export function useSchemaVersions(
  documentTypeId: number | undefined,
  options?: FormOptions<SchemaVersionSummaryResponse[]>,
) {
  const departmentId = options?.departmentId ?? null
  return useQuery({
    queryKey: schemaKeys.versions(documentTypeId ?? 0, departmentId),
    queryFn: () => schemaApi.versions(documentTypeId ?? 0, departmentId),
    enabled: documentTypeId !== undefined && (options?.enabled ?? true),
  })
}

/** One schema version with its fields (administration). */
export function useSchemaVersion(
  schemaVersionId: string | undefined,
  options?: Partial<Pick<UseQueryOptions<SchemaVersionResponse>, 'enabled'>>,
) {
  return useQuery({
    queryKey: schemaKeys.version(schemaVersionId ?? ''),
    queryFn: () => schemaApi.version(schemaVersionId ?? ''),
    enabled: Boolean(schemaVersionId) && (options?.enabled ?? true),
  })
}

/**
 * The published schema of a form: the create form, or with `departmentId` the record form that
 * department fills on receipt. `data.schema` is null when the form has none.
 */
export function useDocumentTypeSchema(
  documentTypeId: number | undefined,
  options?: FormOptions<DocumentTypeSchemaResponse>,
) {
  const departmentId = options?.departmentId ?? null
  return useQuery({
    queryKey: schemaKeys.typeSchema(documentTypeId ?? 0, departmentId),
    queryFn: () => schemaApi.typeSchema(documentTypeId ?? 0, departmentId),
    staleTime: 30 * 1000,
    enabled: documentTypeId !== undefined && (options?.enabled ?? true),
  })
}

/** A document's pinned schema version and stored values. */
export function useDocumentFields(
  documentId: string | undefined,
  options?: Partial<Pick<UseQueryOptions<DocumentFieldsResponse>, 'enabled'>>,
) {
  return useQuery({
    queryKey: schemaKeys.documentFields(documentId ?? ''),
    queryFn: () => schemaApi.documentFields(documentId ?? ''),
    enabled: Boolean(documentId) && (options?.enabled ?? true),
  })
}
