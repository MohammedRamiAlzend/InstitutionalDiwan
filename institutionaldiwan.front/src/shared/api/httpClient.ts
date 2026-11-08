import axios, { type AxiosProgressEvent } from 'axios'
import { ENV } from '@/shared/config'
import { tokenStorage } from './tokenStorage'
import { ApiError, toApiErrorAsync } from './problemDetails'
import { CLIENT_ERROR_KIND, reportClientError } from './errorReporter'

/**
 * The single Axios instance for all API calls. Attaches the bearer token,
 * and normalizes every failure to `ApiError`. A `401` clears the session and
 * sends the user back to `/login`.
 */
export const httpClient = axios.create({
  baseURL: ENV.API_BASE_URL,
})

httpClient.interceptors.request.use((config) => {
  const token = tokenStorage.get()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

httpClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const apiError = await toApiErrorAsync(error)

    // 0 means no answer at all; the deliberate cancel of a superseded query is not a failure.
    if ((apiError.status === 0 || apiError.status >= 500) && !axios.isCancel(error)) {
      const method = axios.isAxiosError(error) ? error.config?.method?.toUpperCase() : undefined
      const url = axios.isAxiosError(error) ? error.config?.url : undefined
      reportClientError({
        kind: CLIENT_ERROR_KIND.Network,
        message: `${method ?? 'Request'} ${url?.split('?')[0] ?? ''} failed (${apiError.status || 'no response'})`,
        path: url,
        httpMethod: method,
        statusCode: apiError.status || undefined,
      })
    }

    if (apiError.status === 401 && !window.location.pathname.startsWith('/login')) {
      tokenStorage.clear()
      window.location.assign('/login')
    }

    return Promise.reject(apiError)
  },
)

export interface UploadOptions {
  /** 0–100 while the body is being sent; not called when the browser does not know the total. */
  onProgress?: (percent: number) => void
}

/**
 * Multipart POST for the Module 06 uploads. `Content-Type` is deliberately left unset so the
 * browser writes the multipart boundary; the JWT interceptor and error normalization still apply.
 */
export async function uploadForm<T>(
  url: string,
  form: FormData,
  options: UploadOptions = {},
): Promise<T> {
  const { data } = await httpClient.post<T>(url, form, {
    onUploadProgress: (event: AxiosProgressEvent) => {
      if (!options.onProgress || !event.total) return
      options.onProgress(Math.round((event.loaded / event.total) * 100))
    },
  })
  return data
}

export interface BlobDownload {
  blob: Blob
  /** Raw `Content-Disposition`; resolve it with `parseContentDisposition` from `@/shared/lib`. */
  contentDisposition: string | null
}

/**
 * Binary GET for file downloads. A failure arrives as a blob-bodied ProblemDetails and is
 * normalized by the response interceptor, so callers still receive an `ApiError` with the domain
 * code (for example `Attachments.FileNotFound`) and an Arabic message.
 */
export async function downloadBlob(
  url: string,
  params?: Record<string, unknown>,
): Promise<BlobDownload> {
  const response = await httpClient.get<Blob>(url, { params, responseType: 'blob' })
  const header = response.headers?.['content-disposition']

  return {
    blob: response.data,
    contentDisposition: typeof header === 'string' ? header : null,
  }
}

export { ApiError }
