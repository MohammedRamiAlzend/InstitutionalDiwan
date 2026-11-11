import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  decisionTypeKeys,
  type DecisionTypeResponse,
} from '@/entities/decision-type'
import { httpClient } from '@/shared/api'
import type { DecisionTypeWriteRequest } from '../model/types'

function useInvalidateTypes() {
  const queryClient = useQueryClient()
  return () => {
    void queryClient.invalidateQueries({ queryKey: decisionTypeKeys.all })
  }
}

export function useCreateDecisionTypeMutation() {
  const invalidate = useInvalidateTypes()
  return useMutation({
    mutationFn: async (request: DecisionTypeWriteRequest): Promise<DecisionTypeResponse> => {
      const { data } = await httpClient.post<DecisionTypeResponse>('/decision-types', request)
      return data
    },
    onSuccess: invalidate,
  })
}

export function useUpdateDecisionTypeMutation(decisionTypeId: string) {
  const invalidate = useInvalidateTypes()
  return useMutation({
    mutationFn: async (request: DecisionTypeWriteRequest): Promise<DecisionTypeResponse> => {
      const { data } = await httpClient.put<DecisionTypeResponse>(
        `/decision-types/${decisionTypeId}`,
        request,
      )
      return data
    },
    onSuccess: invalidate,
  })
}

export function useActivateDecisionTypeMutation() {
  const invalidate = useInvalidateTypes()
  return useMutation({
    mutationFn: async (decisionTypeId: string): Promise<DecisionTypeResponse> => {
      const { data } = await httpClient.post<DecisionTypeResponse>(
        `/decision-types/${decisionTypeId}/activate`,
      )
      return data
    },
    onSuccess: invalidate,
  })
}

export function useDeactivateDecisionTypeMutation() {
  const invalidate = useInvalidateTypes()
  return useMutation({
    mutationFn: async (decisionTypeId: string): Promise<DecisionTypeResponse> => {
      const { data } = await httpClient.post<DecisionTypeResponse>(
        `/decision-types/${decisionTypeId}/deactivate`,
      )
      return data
    },
    onSuccess: invalidate,
  })
}
