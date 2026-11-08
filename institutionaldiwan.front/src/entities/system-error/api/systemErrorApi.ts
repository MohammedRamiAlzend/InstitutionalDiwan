import { downloadBlob, httpClient, type BlobDownload } from '@/shared/api'
import type { PagedResult } from '@/shared/types'
import type {
  SystemErrorDetailsResponse,
  SystemErrorFilters,
  SystemErrorListParams,
  SystemErrorSummaryResponse,
} from '../model/types'

export const systemErrorApi = {
  /** `GET /api/system-errors` — `SYSTEM_ERRORS_VIEW`. */
  async list(params: SystemErrorListParams): Promise<PagedResult<SystemErrorSummaryResponse>> {
    const { data } = await httpClient.get<PagedResult<SystemErrorSummaryResponse>>('/system-errors', { params })
    return data
  },

  /** `GET /api/system-errors/{id}` — with the stack trace of the latest occurrence. */
  async details(id: string): Promise<SystemErrorDetailsResponse> {
    const { data } = await httpClient.get<SystemErrorDetailsResponse>(`/system-errors/${id}`)
    return data
  },

  /** `GET /api/system-errors/export` — an `.xlsx` of what the filters match. */
  exportFile(filters: SystemErrorFilters): Promise<BlobDownload> {
    return downloadBlob('/system-errors/export', { ...filters })
  },
}
