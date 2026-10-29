import { useMutation, useQueryClient, type QueryClient } from '@tanstack/react-query'
import { httpClient } from '@/shared/api'
import {
  externalOrganizationKeys,
  type ExternalOrganizationResponse,
} from '@/entities/external-organization'
import type {
  CreateExternalOrganizationRequest,
  UpdateExternalOrganizationRequest,
} from '../model/types'

function invalidateExternalOrganizations(
  queryClient: QueryClient,
  externalOrganizationId?: string,
) {
  void queryClient.invalidateQueries({ queryKey: externalOrganizationKeys.all })
  if (externalOrganizationId) {
    void queryClient.invalidateQueries({
      queryKey: externalOrganizationKeys.detail(externalOrganizationId),
    })
  }
}

/** `POST /api/external-organizations` — registers an external correspondent. */
export function useCreateExternalOrganizationMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (
      request: CreateExternalOrganizationRequest,
    ): Promise<ExternalOrganizationResponse> => {
      const { data } = await httpClient.post<ExternalOrganizationResponse>(
        '/external-organizations',
        request,
      )
      return data
    },
    onSuccess: () => invalidateExternalOrganizations(queryClient),
  })
}

/** `PUT /api/external-organizations/{id}` — updates the descriptive fields. */
export function useUpdateExternalOrganizationMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      externalOrganizationId,
      request,
    }: {
      externalOrganizationId: string
      request: UpdateExternalOrganizationRequest
    }): Promise<ExternalOrganizationResponse> => {
      const { data } = await httpClient.put<ExternalOrganizationResponse>(
        `/external-organizations/${externalOrganizationId}`,
        request,
      )
      return data
    },
    onSuccess: (_data, variables) =>
      invalidateExternalOrganizations(queryClient, variables.externalOrganizationId),
  })
}

/** `DELETE /api/external-organizations/{id}` (deactivate) and `POST /{id}/activate`. */
export function useSetExternalOrganizationActiveMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      externalOrganizationId,
      active,
    }: {
      externalOrganizationId: string
      active: boolean
    }): Promise<ExternalOrganizationResponse> => {
      const { data } = active
        ? await httpClient.post<ExternalOrganizationResponse>(
            `/external-organizations/${externalOrganizationId}/activate`,
          )
        : await httpClient.delete<ExternalOrganizationResponse>(
            `/external-organizations/${externalOrganizationId}`,
          )
      return data
    },
    onSuccess: (_data, variables) =>
      invalidateExternalOrganizations(queryClient, variables.externalOrganizationId),
  })
}
