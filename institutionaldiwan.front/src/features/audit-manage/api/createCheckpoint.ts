import { useMutation, useQueryClient } from '@tanstack/react-query'
import { auditLogKeys, type AuditCheckpointResponse } from '@/entities/audit-log'
import { httpClient } from '@/shared/api'
import type { CreateCheckpointRequest } from '../model/types'

/** `POST /api/audit-logs/checkpoints` — seals every entry since the last anchor. */
export function useCreateCheckpointMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (
      request: CreateCheckpointRequest,
    ): Promise<AuditCheckpointResponse> => {
      const { data } = await httpClient.post<AuditCheckpointResponse>(
        '/audit-logs/checkpoints',
        request,
      )
      return data
    },
    // The sealed range is fixed, but the chain (and its audit view row) moved on.
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: auditLogKeys.all })
    },
  })
}
