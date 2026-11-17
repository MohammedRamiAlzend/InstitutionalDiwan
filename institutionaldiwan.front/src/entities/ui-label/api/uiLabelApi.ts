import { httpClient } from '@/shared/api'
import type { UiLabelOverridesResponse } from '../model/types'

/** Authenticated read of the system-wide label overrides; works for every signed-in account. */
export const uiLabelApi = {
  get: async (): Promise<UiLabelOverridesResponse> => {
    const { data } = await httpClient.get<UiLabelOverridesResponse>('/ui-labels')
    return data
  },
}