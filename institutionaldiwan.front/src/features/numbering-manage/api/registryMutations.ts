import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  numberingRegistryKeys,
  type NumberingRegistryResponse,
} from '@/entities/numbering-registry'
import { httpClient } from '@/shared/api'
import type {
  CreateNumberingRegistryRequest,
  UpdateNumberingRegistryRequest,
} from '../model/types'

/** Every registry command changes the registry list and its counters. */
function useRegistryInvalidation() {
  const queryClient = useQueryClient()

  return () => {
    void queryClient.invalidateQueries({ queryKey: numberingRegistryKeys.all })
  }
}

/** `POST /api/numbering/registries`. */
export function useCreateNumberingRegistryMutation() {
  const invalidate = useRegistryInvalidation()

  return useMutation({
    mutationFn: async (
      request: CreateNumberingRegistryRequest,
    ): Promise<NumberingRegistryResponse> => {
      const { data } = await httpClient.post<NumberingRegistryResponse>(
        '/numbering/registries',
        request,
      )
      return data
    },
    onSuccess: invalidate,
  })
}

/** `PUT /api/numbering/registries/{registryId}` — rejects template changes after issuance. */
export function useUpdateNumberingRegistryMutation(registryId: string) {
  const invalidate = useRegistryInvalidation()

  return useMutation({
    mutationFn: async (
      request: UpdateNumberingRegistryRequest,
    ): Promise<NumberingRegistryResponse> => {
      const { data } = await httpClient.put<NumberingRegistryResponse>(
        `/numbering/registries/${registryId}`,
        request,
      )
      return data
    },
    onSuccess: invalidate,
  })
}

/** Deactivate (`DELETE`) or reactivate (`POST .../activate`) a registry. */
export function useSetNumberingRegistryActiveMutation() {
  const invalidate = useRegistryInvalidation()

  return useMutation({
    mutationFn: async ({
      registryId,
      active,
    }: {
      registryId: string
      active: boolean
    }): Promise<NumberingRegistryResponse> => {
      const { data } = active
        ? await httpClient.post<NumberingRegistryResponse>(
            `/numbering/registries/${registryId}/activate`,
          )
        : await httpClient.delete<NumberingRegistryResponse>(
            `/numbering/registries/${registryId}`,
          )
      return data
    },
    onSuccess: invalidate,
  })
}
