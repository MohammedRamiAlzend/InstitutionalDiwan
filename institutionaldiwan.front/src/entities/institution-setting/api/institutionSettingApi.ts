import { httpClient } from '@/shared/api'
import type { InstitutionSettingResponse } from '../model/types'

/** Typed read call for the singleton institution settings (requires `DEPARTMENT_MANAGE`). */
export const institutionSettingApi = {
  get: async (): Promise<InstitutionSettingResponse> => {
    const { data } = await httpClient.get<InstitutionSettingResponse>(
      '/institution-settings',
    )
    return data
  },
}
