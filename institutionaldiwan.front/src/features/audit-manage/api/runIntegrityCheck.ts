import { useMutation, useQueryClient } from '@tanstack/react-query'
import { auditLogKeys, type AuditIntegrityCheckResponse } from '@/entities/audit-log'
import { httpClient } from '@/shared/api'
import type { RunIntegrityCheckRequest } from '../model/types'

/**
 * `POST /api/audit-logs/integrity-checks`. A broken chain is **HTTP 200** with
 * `isValid: false` — a successful mutation, not a transport failure.
 */
export function useRunIntegrityCheckMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (
      request: RunIntegrityCheckRequest,
    ): Promise<AuditIntegrityCheckResponse> => {
      const { data } = await httpClient.post<AuditIntegrityCheckResponse>(
        '/audit-logs/integrity-checks',
        request,
      )
      return data
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: auditLogKeys.all })
    },
  })
}
