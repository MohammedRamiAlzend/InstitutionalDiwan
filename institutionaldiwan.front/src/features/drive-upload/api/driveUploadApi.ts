import { httpClient } from '@/shared/api'
import type { DriveVersionResponse } from '@/entities/drive-item'
import type {
  CompleteDriveUploadRequest,
  DriveUploadInitiatedResponse,
  InitiateDriveUploadRequest,
} from '../model/types'

/**
 * The three presigned-upload writes (Module 11). Bytes never pass through these routes: `initiate`
 * signs a direct-to-SeaweedFS PUT, the browser uploads with `putPresigned`, then `complete` verifies
 * and commits (or `abandon` cleans up a failed/cancelled reservation). Routes are relative to `/api`.
 */
export const driveUploadApi = {
  initiate: async (request: InitiateDriveUploadRequest): Promise<DriveUploadInitiatedResponse> => {
    const { data } = await httpClient.post<DriveUploadInitiatedResponse>(
      '/drive/files/initiate',
      request,
    )
    return data
  },

  complete: async (
    versionId: string,
    request: CompleteDriveUploadRequest,
  ): Promise<DriveVersionResponse> => {
    const { data } = await httpClient.post<DriveVersionResponse>(
      `/drive/files/${versionId}/complete`,
      request,
    )
    return data
  },

  abandon: async (versionId: string): Promise<void> => {
    await httpClient.delete(`/drive/files/${versionId}/abandon`)
  },
}