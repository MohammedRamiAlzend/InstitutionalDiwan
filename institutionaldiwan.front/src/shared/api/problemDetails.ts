import { AxiosError } from 'axios'
import type { ProblemDetails } from '@/shared/types'

/**
 * Normalized API failure. `code` carries the ProblemDetails `title` (the domain
 * error code), `detail` the human-readable message. Branch UI on `code`, never
 * on the HTTP status alone.
 */
export class ApiError extends Error {
  readonly code: string
  readonly detail: string
  readonly status: number

  constructor(code: string, detail: string, status: number) {
    super(detail)
    this.name = 'ApiError'
    this.code = code
    this.detail = detail
    this.status = status
  }
}

export function toApiError(error: unknown): ApiError {
  if (error instanceof ApiError) return error

  if (error instanceof AxiosError) {
    const problem = error.response?.data as ProblemDetails | undefined
    const status = error.response?.status ?? 0
    if (problem && typeof problem === 'object') {
      return new ApiError(
        problem.title ?? 'Unknown',
        problem.detail ?? error.message,
        status,
      )
    }
    return new ApiError('NetworkError', 'تعذّر الاتصال بالخادم.', status)
  }

  return new ApiError('Unknown', 'حدث خطأ غير متوقع.', 0)
}

export function isConflictError(error: unknown, expectedCode?: string): boolean {
  return (
    error instanceof ApiError &&
    error.status === 409 &&
    (expectedCode === undefined || error.code === expectedCode)
  )
}
