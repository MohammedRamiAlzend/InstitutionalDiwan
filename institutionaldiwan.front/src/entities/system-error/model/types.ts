import type { PagedQueryParams } from '@/shared/types'

/** `SystemErrorStatus` — the API serializes enums as integers. */
export const SYSTEM_ERROR_STATUS = { New: 1, InProgress: 2, Resolved: 3 } as const
export type SystemErrorStatus = (typeof SYSTEM_ERROR_STATUS)[keyof typeof SYSTEM_ERROR_STATUS]

/** `SystemErrorSource`. */
export const SYSTEM_ERROR_SOURCE = { Server: 1, Client: 2 } as const
export type SystemErrorSource = (typeof SYSTEM_ERROR_SOURCE)[keyof typeof SYSTEM_ERROR_SOURCE]

/** `SystemErrorKind`. */
export const SYSTEM_ERROR_KIND = {
  ServerException: 1,
  ServerRejection: 2,
  ClientRender: 3,
  ClientScript: 4,
  ClientNetwork: 5,
} as const
export type SystemErrorKind = (typeof SYSTEM_ERROR_KIND)[keyof typeof SYSTEM_ERROR_KIND]

export const SYSTEM_ERROR_NOTES_MAX_LENGTH = 4000

/** `SystemErrorSummaryResponse` — a row of the error list. */
export interface SystemErrorSummaryResponse {
  systemErrorId: string
  source: SystemErrorSource
  kind: SystemErrorKind
  title: string
  message: string
  path: string | null
  httpMethod: string | null
  statusCode: number | null
  lastUsername: string | null
  occurrenceCount: number
  firstSeenAt: string
  lastSeenAt: string
  status: SystemErrorStatus
}

/** `SystemErrorDetailsResponse` — one error with the technical details of its latest occurrence. */
export interface SystemErrorDetailsResponse {
  summary: SystemErrorSummaryResponse
  details: string | null
  userAgent: string | null
  traceId: string | null
  notes: string | null
  resolvedAt: string | null
}

export type SystemErrorSortKey = 'lastSeenAt' | 'firstSeenAt' | 'occurrenceCount' | 'title' | 'status'

/** The filters shared by the list and the Excel export; dates are UTC ISO instants. */
export interface SystemErrorFilters {
  search?: string
  sortBy?: SystemErrorSortKey
  sortDescending?: boolean
  status?: number
  source?: number
  kind?: number
  lastSeenFromUtc?: string
  lastSeenToUtc?: string
}

export type SystemErrorListParams = PagedQueryParams & SystemErrorFilters
