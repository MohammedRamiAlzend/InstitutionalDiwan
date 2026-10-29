import { useQuery, type UseQueryOptions } from '@tanstack/react-query'
import { movementApi } from '../api/movementApi'
import type {
  MovementQueueItemResponse,
  MovementQueueKind,
  MovementResponse,
} from './types'

export const movementKeys = {
  all: ['movements'] as const,
  inbox: (departmentId?: string) =>
    [...movementKeys.all, 'inbox', departmentId ?? 'me'] as const,
  outbox: (departmentId?: string) =>
    [...movementKeys.all, 'outbox', departmentId ?? 'me'] as const,
  byDocument: (documentId: string) =>
    [...movementKeys.all, 'by-document', documentId] as const,
  detail: (movementId: string) => [...movementKeys.all, 'detail', movementId] as const,
}

type EnabledOptions<T> = Partial<Pick<UseQueryOptions<T>, 'enabled'>>

/** A department queue; without an id the API resolves the caller's primary department. */
export function useMovementQueue(
  kind: MovementQueueKind,
  departmentId?: string,
  options?: EnabledOptions<MovementQueueItemResponse[]>,
) {
  return useQuery({
    queryKey:
      kind === 'inbox'
        ? movementKeys.inbox(departmentId)
        : movementKeys.outbox(departmentId),
    queryFn: () =>
      kind === 'inbox'
        ? movementApi.inbox(departmentId)
        : movementApi.outbox(departmentId),
    staleTime: 15 * 1000,
    ...options,
  })
}

export function useMovementInbox(
  departmentId?: string,
  options?: EnabledOptions<MovementQueueItemResponse[]>,
) {
  return useMovementQueue('inbox', departmentId, options)
}

export function useMovementOutbox(
  departmentId?: string,
  options?: EnabledOptions<MovementQueueItemResponse[]>,
) {
  return useMovementQueue('outbox', departmentId, options)
}

/** Every movement of a document, newest first, with steps and events. */
export function useDocumentMovements(
  documentId: string | null | undefined,
  options?: EnabledOptions<MovementResponse[]>,
) {
  return useQuery({
    queryKey: movementKeys.byDocument(documentId ?? ''),
    queryFn: () => movementApi.byDocument(documentId as string),
    staleTime: 15 * 1000,
    ...options,
    enabled: Boolean(documentId) && (options?.enabled ?? true),
  })
}

export function useMovementById(
  movementId: string | null | undefined,
  options?: EnabledOptions<MovementResponse>,
) {
  return useQuery({
    queryKey: movementKeys.detail(movementId ?? ''),
    queryFn: () => movementApi.byId(movementId as string),
    staleTime: 15 * 1000,
    ...options,
    enabled: Boolean(movementId) && (options?.enabled ?? true),
  })
}
