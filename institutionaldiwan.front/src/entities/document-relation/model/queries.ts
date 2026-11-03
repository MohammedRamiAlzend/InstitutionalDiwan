import { useQuery, type UseQueryOptions } from '@tanstack/react-query'
import { relationApi } from '../api/relationApi'
import type {
  DocumentRelationsResponse,
  RelationEventsPage,
  RelationGraphResponse,
} from './types'

export const relationKeys = {
  all: ['document-relations'] as const,
  forDocument: (documentId: string) => [...relationKeys.all, documentId] as const,
  list: (documentId: string) => [...relationKeys.forDocument(documentId), 'list'] as const,
  graph: (documentId: string, depth: number) =>
    [...relationKeys.forDocument(documentId), 'graph', depth] as const,
  events: (documentId: string, page: number, pageSize: number) =>
    [...relationKeys.forDocument(documentId), 'events', page, pageSize] as const,
}

type EnabledOptions<T> = Partial<Pick<UseQueryOptions<T>, 'enabled'>>

/** A document's links, in both directions. */
export function useDocumentRelations(
  documentId: string | undefined,
  options?: EnabledOptions<DocumentRelationsResponse>,
) {
  return useQuery({
    queryKey: relationKeys.list(documentId ?? ''),
    queryFn: () => relationApi.list(documentId ?? ''),
    staleTime: 30 * 1000,
    enabled: Boolean(documentId) && (options?.enabled ?? true),
  })
}

/** Documents reachable from this one through links the caller may see. */
export function useRelationGraph(
  documentId: string | undefined,
  depth: number,
  options?: EnabledOptions<RelationGraphResponse>,
) {
  return useQuery({
    queryKey: relationKeys.graph(documentId ?? '', depth),
    queryFn: () => relationApi.graph(documentId ?? '', depth),
    placeholderData: (previous) => previous,
    staleTime: 30 * 1000,
    enabled: Boolean(documentId) && (options?.enabled ?? true),
  })
}

/** The permanent relation history of a document, newest first. */
export function useRelationEvents(
  documentId: string | undefined,
  page: number,
  pageSize: number,
  options?: EnabledOptions<RelationEventsPage>,
) {
  return useQuery({
    queryKey: relationKeys.events(documentId ?? '', page, pageSize),
    queryFn: () => relationApi.events(documentId ?? '', page, pageSize),
    placeholderData: (previous) => previous,
    enabled: Boolean(documentId) && (options?.enabled ?? true),
  })
}
