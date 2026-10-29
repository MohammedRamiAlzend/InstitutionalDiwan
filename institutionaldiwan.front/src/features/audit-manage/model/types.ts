/** `POST /api/audit-logs/checkpoints` — omit the id to seal through the latest entry. */
export interface CreateCheckpointRequest {
  toAuditLogId: number | null
}

/** `POST /api/audit-logs/integrity-checks` — omit both ids to verify the whole chain. */
export interface RunIntegrityCheckRequest {
  startAuditLogId: number | null
  endAuditLogId: number | null
}
