import type { PagedQueryParams } from '@/shared/types'

/** Server sort keys accepted by `GET /api/audit-logs`. */
export type AuditLogSortKey = 'occurredAt' | 'module' | 'action' | 'entity'

/** `AuditLogResponse` — one append-only entry of the tamper-evident chain. */
export interface AuditLogResponse {
  auditLogId: number
  occurredAtUtc: string
  actorUserId: string | null
  actorDepartmentId: string | null
  moduleName: string
  actionTypeId: number
  entityTypeId: number
  entityId: string
  relatedDocumentId: string | null
  oldValuesJson: string | null
  newValuesJson: string | null
  ipAddress: string | null
  correlationId: string | null
  previousHash: string
  eventHash: string
}

/** `AuditIntegrityCheckResponse` — a persisted chain walk; `isValid: false` means tampering. */
export interface AuditIntegrityCheckResponse {
  checkId: string
  startAuditLogId: number
  endAuditLogId: number
  isValid: boolean
  firstCorruptedLogId: number | null
  failureDetails: string | null
  executedByUserId: string
  executedAtUtc: string
}

/** `AuditCheckpointResponse` — a sealed anchor over a contiguous range of entries. */
export interface AuditCheckpointResponse {
  checkpointId: string
  fromAuditLogId: number
  toAuditLogId: number
  terminalHash: string
  recordCount: number
  createdByUserId: string
  createdAtUtc: string
}

/** Filters accepted by the audit search endpoint (`sortBy` narrows `PagedQueryParams`). */
export interface AuditLogListParams extends Omit<PagedQueryParams, 'sortBy'> {
  sortBy?: AuditLogSortKey
  moduleName?: string
  actionTypeId?: number
  fromUtc?: string
  toUtc?: string
}

/** The integrity-report list only supports paging. */
export interface AuditIntegrityListParams {
  page?: number
  pageSize?: number
}
