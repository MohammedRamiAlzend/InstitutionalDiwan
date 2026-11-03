import { useQuery, type UseQueryOptions } from '@tanstack/react-query'
import { schemaApi } from '../api/schemaApi'
import type {
  DocumentFieldResponse,
  DocumentFieldsResponse,
  DocumentTypeSchemaResponse,
  SchemaVersionResponse,
  SchemaVersionSummaryResponse,
} from './types'

export const schemaKeys = {
  all: ['document-schema'] as const,
  fields: (documentTypeId: number) => [...schemaKeys.all, 'fields', documentTypeId] as const,
  versions: (documentTypeId: number) => [...schemaKeys.all, 'versions', documentTypeId] as const,
  version: (schemaVersionId: string) => [...schemaKeys.all, 'version', schemaVersionId] as const,
  typeSchema: (documentTypeId: number) =>
    [...schemaKeys.all, 'type-schema', documentTypeId] as const,
  documentFields: (documentId: string) => [...schemaKeys.all, 'document', documentId] as const,
}

type EnabledOptions<T> = Partial<Pick<UseQueryOptions<T>, 'enabled'>>

/** The fields defined for a document type (administration). */
export function useDocumentTypeFields(
  documentTypeId: number | undefined,
  options?: EnabledOptions<DocumentFieldResponse[]>,
) {
  return useQuery({
    queryKey: schemaKeys.fields(documentTypeId ?? 0),
    queryFn: () => schemaApi.fields(documentTypeId ?? 0),
    enabled: documentTypeId !== undefined && (options?.enabled ?? true),
  })
}

/** The schema version history of a document type, newest first (administration). */
export function useSchemaVersions(
  documentTypeId: number | undefined,
  options?: EnabledOptions<SchemaVersionSummaryResponse[]>,
) {
  return useQuery({
    queryKey: schemaKeys.versions(documentTypeId ?? 0),
    queryFn: () => schemaApi.versions(documentTypeId ?? 0),
    enabled: documentTypeId !== undefined && (options?.enabled ?? true),
  })
}

/** One schema version with its fields (administration). */
export function useSchemaVersion(
  schemaVersionId: string | undefined,
  options?: EnabledOptions<SchemaVersionResponse>,
) {
  return useQuery({
    queryKey: schemaKeys.version(schemaVersionId ?? ''),
    queryFn: () => schemaApi.version(schemaVersionId ?? ''),
    enabled: Boolean(schemaVersionId) && (options?.enabled ?? true),
  })
}

/** The published schema that renders the create form; `data.schema` is null when the type has none. */
export function useDocumentTypeSchema(
  documentTypeId: number | undefined,
  options?: EnabledOptions<DocumentTypeSchemaResponse>,
) {
  return useQuery({
    queryKey: schemaKeys.typeSchema(documentTypeId ?? 0),
    queryFn: () => schemaApi.typeSchema(documentTypeId ?? 0),
    staleTime: 30 * 1000,
    enabled: documentTypeId !== undefined && (options?.enabled ?? true),
  })
}

/** A document's pinned schema version and stored values. */
export function useDocumentFields(
  documentId: string | undefined,
  options?: EnabledOptions<DocumentFieldsResponse>,
) {
  return useQuery({
    queryKey: schemaKeys.documentFields(documentId ?? ''),
    queryFn: () => schemaApi.documentFields(documentId ?? ''),
    enabled: Boolean(documentId) && (options?.enabled ?? true),
  })
}
