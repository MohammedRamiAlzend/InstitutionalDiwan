import { useQuery, type UseQueryOptions } from '@tanstack/react-query'
import type { PagedResult } from '@/shared/types'
import { listAuditLogs } from '../api/listAuditLogs'
import { listIntegrityReports } from '../api/listIntegrityReports'
import type {
  AuditIntegrityCheckResponse,
  AuditIntegrityListParams,
  AuditLogListParams,
  AuditLogResponse,
} from './types'

export const auditLogKeys = {
  all: ['audit-logs'] as const,
  list: (params: AuditLogListParams) => [...auditLogKeys.all, 'list', params] as const,
  integrity: (params: AuditIntegrityListParams) =>
    [...auditLogKeys.all, 'integrity', params] as const,
}

type EnabledOptions<T> = Partial<Pick<UseQueryOptions<T>, 'enabled'>>

/**
 * Reading the audit trail appends a self-audit row, so the defaults favor a
 * long `staleTime` and user-driven refetch over automatic refetching.
 */
const READ_STALE_MS = 5 * 60 * 1000

export function useAuditLogs(
  params: AuditLogListParams,
  options?: EnabledOptions<PagedResult<AuditLogResponse>>,
) {
  return useQuery({
    queryKey: auditLogKeys.list(params),
    queryFn: () => listAuditLogs(params),
    placeholderData: (previous) => previous,
    staleTime: READ_STALE_MS,
    refetchOnWindowFocus: false,
    ...options,
  })
}

export function useAuditIntegrityReports(
  params: AuditIntegrityListParams,
  options?: EnabledOptions<PagedResult<AuditIntegrityCheckResponse>>,
) {
  return useQuery({
    queryKey: auditLogKeys.integrity(params),
    queryFn: () => listIntegrityReports(params),
    placeholderData: (previous) => previous,
    staleTime: READ_STALE_MS,
    refetchOnWindowFocus: false,
    ...options,
  })
}
