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

/** Reads a ProblemDetails body that is still an unread `Blob`, or `null` when it is not JSON. */
async function readBlobProblemDetails(blob: Blob): Promise<ProblemDetails | null> {
  if (!blob.type.includes('json')) return null
  try {
    const parsed: unknown = JSON.parse(await blob.text())
    return parsed && typeof parsed === 'object' ? (parsed as ProblemDetails) : null
  } catch {
    return null
  }
}

/**
 * `toApiError` for a response whose body may still be a `Blob`. A download asks for
 * `responseType: 'blob'`, so a failure carries its ProblemDetails JSON inside the blob instead of a
 * parsed object; without this the domain code is lost and the UI shows the English axios message.
 */
export async function toApiErrorAsync(error: unknown): Promise<ApiError> {
  if (error instanceof AxiosError && error.response?.data instanceof Blob) {
    const status = error.response.status
    const problem = await readBlobProblemDetails(error.response.data)
    return new ApiError(problem?.title ?? 'Unknown', problem?.detail ?? error.message, status)
  }
  return toApiError(error)
}
