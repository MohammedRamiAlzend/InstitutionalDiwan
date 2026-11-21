import { httpClient } from '@/shared/api'
import type {
  DriveSettingsResponse,
  DriveSpaceResponse,
  DriveSpacesOverviewResponse,
} from '../model/types'

/**
 * Typed reads for Module 11 Drive spaces. Writes (create/rename/quota/ensure-personal/settings-update)
 * live in `features/drive-space-manage`. Routes are relative to the `/api` baseURL.
 */
export const driveSpaceApi = {
  /** The caller's personal space plus every shared space they can reach (`DRIVE_USE`). */
  overview: async (): Promise<DriveSpacesOverviewResponse> => {
    const { data } = await httpClient.get<DriveSpacesOverviewResponse>('/drive/spaces')
    return data
  },

  /** One space with its effective quota and usage (owner / grant / ADMIN). */
  get: async (spaceId: string): Promise<DriveSpaceResponse> => {
    const { data } = await httpClient.get<DriveSpaceResponse>(`/drive/spaces/${spaceId}`)
    return data
  },

  /** Institution Drive defaults (`DRIVE_MANAGE`); a normal user gets 403, so this read is opt-in. */
  settings: async (): Promise<DriveSettingsResponse> => {
    const { data } = await httpClient.get<DriveSettingsResponse>('/drive/settings')
    return data
  },
}