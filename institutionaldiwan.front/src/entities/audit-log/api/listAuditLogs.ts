import { httpClient } from '@/shared/api'
import type { PagedResult } from '@/shared/types'
import type { AuditLogListParams, AuditLogResponse } from '../model/types'

/**
 * `GET /api/audit-logs` — paged, filtered audit search. Side-effecting on the
 * server: every call appends a self-audit `View` entry, so callers must not poll.
 */
export async function listAuditLogs(
  params: AuditLogListParams,
): Promise<PagedResult<AuditLogResponse>> {
  const { data } = await httpClient.get<PagedResult<AuditLogResponse>>('/audit-logs', {
    params,
  })
  return data
}
