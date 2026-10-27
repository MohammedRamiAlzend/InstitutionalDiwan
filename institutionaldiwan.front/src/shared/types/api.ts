/** RFC 7807 problem details returned by the API on every failure. */
export interface ProblemDetails {
  type?: string
  /** Domain error code (e.g. `Numbering.OfficialNumberTaken`) — branch on this, not the status. */
  title?: string
  status?: number
  detail?: string
  instance?: string
  errors?: Record<string, string[]>
}

/** Shared paged list envelope returned by paged endpoints. */
export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasPrevious: boolean
  hasNext: boolean
}

/** Common URL-driven list query params (filters/search/sort/page live in the URL). */
export interface PagedQueryParams {
  page?: number
  pageSize?: number
  search?: string
  sortBy?: string
  sortDescending?: boolean
}
