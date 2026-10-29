import { useMutation, useQueryClient } from '@tanstack/react-query'
import { documentKeys } from '@/entities/document'
import {
  movementKeys,
  type MovementReceiptResponse,
  type MovementResponse,
} from '@/entities/movement'
import { httpClient } from '@/shared/api'
import type {
  CreateMovementRequest,
  MovementNotesRequest,
  MovementReasonRequest,
  RedirectMovementRequest,
} from '../model/types'

/** Every movement action changes the document status, its timeline, and both department queues. */
function useMovementInvalidation() {
  const queryClient = useQueryClient()

  return () => {
    void queryClient.invalidateQueries({ queryKey: movementKeys.all })
    void queryClient.invalidateQueries({ queryKey: documentKeys.all })
  }
}

async function postMovementAction<T>(
  movementId: string,
  action: string,
  body: unknown,
): Promise<T> {
  const { data } = await httpClient.post<T>(`/movements/${movementId}/${action}`, body)
  return data
}

/** `POST /api/movements` — opens a draft movement from the caller's primary department. */
export function useCreateMovementMutation() {
  const invalidate = useMovementInvalidation()

  return useMutation({
    mutationFn: async (request: CreateMovementRequest): Promise<MovementResponse> => {
      const { data } = await httpClient.post<MovementResponse>('/movements', request)
      return data
    },
    onSuccess: invalidate,
  })
}

export function useSendMovementMutation() {
  const invalidate = useMovementInvalidation()

  return useMutation({
    mutationFn: ({ movementId, ...body }: { movementId: string } & MovementNotesRequest) =>
      postMovementAction<MovementResponse>(movementId, 'send', body),
    onSuccess: invalidate,
  })
}

/** The receipt carries the destination department's register number. */
export function useReceiveMovementMutation() {
  const invalidate = useMovementInvalidation()

  return useMutation({
    mutationFn: ({ movementId, ...body }: { movementId: string } & MovementNotesRequest) =>
      postMovementAction<MovementReceiptResponse>(movementId, 'receive', body),
    onSuccess: invalidate,
  })
}

export function useCompleteMovementMutation() {
  const invalidate = useMovementInvalidation()

  return useMutation({
    mutationFn: ({ movementId, ...body }: { movementId: string } & MovementNotesRequest) =>
      postMovementAction<MovementResponse>(movementId, 'complete', body),
    onSuccess: invalidate,
  })
}

/** Rejection is routed back to the Central Diwan unless the rejecting department is the Diwan. */
export function useRejectMovementMutation() {
  const invalidate = useMovementInvalidation()

  return useMutation({
    mutationFn: ({ movementId, ...body }: { movementId: string } & MovementReasonRequest) =>
      postMovementAction<MovementResponse>(movementId, 'reject', body),
    onSuccess: invalidate,
  })
}

export function useReturnMovementMutation() {
  const invalidate = useMovementInvalidation()

  return useMutation({
    mutationFn: ({ movementId, ...body }: { movementId: string } & MovementReasonRequest) =>
      postMovementAction<MovementResponse>(movementId, 'return', body),
    onSuccess: invalidate,
  })
}

export function useRedirectMovementMutation() {
  const invalidate = useMovementInvalidation()

  return useMutation({
    mutationFn: ({ movementId, ...body }: { movementId: string } & RedirectMovementRequest) =>
      postMovementAction<MovementResponse>(movementId, 'redirect', body),
    onSuccess: invalidate,
  })
}

export function useCloseMovementMutation() {
  const invalidate = useMovementInvalidation()

  return useMutation({
    mutationFn: ({ movementId, ...body }: { movementId: string } & MovementNotesRequest) =>
      postMovementAction<MovementResponse>(movementId, 'close', body),
    onSuccess: invalidate,
  })
}

export function useCancelMovementMutation() {
  const invalidate = useMovementInvalidation()

  return useMutation({
    mutationFn: ({ movementId, ...body }: { movementId: string } & MovementReasonRequest) =>
      postMovementAction<MovementResponse>(movementId, 'cancel', body),
    onSuccess: invalidate,
  })
}
