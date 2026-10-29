import { httpClient } from '@/shared/api'
import type { PagedResult } from '@/shared/types'
import type { AuditIntegrityCheckResponse, AuditIntegrityListParams } from '../model/types'

/** `GET /api/audit-logs/integrity-checks` — persisted reports, newest first. */
export async function listIntegrityReports(
  params: AuditIntegrityListParams,
): Promise<PagedResult<AuditIntegrityCheckResponse>> {
  const { data } = await httpClient.get<PagedResult<AuditIntegrityCheckResponse>>(
    '/audit-logs/integrity-checks',
    { params },
  )
  return data
}
