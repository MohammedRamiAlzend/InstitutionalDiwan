import { httpClient } from '@/shared/api'
import type { BrandingResponse } from '../model/types'

/** Anonymous read of the institution's public identity, so it works on the sign-in page too. */
export const brandingApi = {
  get: async (): Promise<BrandingResponse> => {
    const { data } = await httpClient.get<BrandingResponse>('/branding')
    return data
  },
}
