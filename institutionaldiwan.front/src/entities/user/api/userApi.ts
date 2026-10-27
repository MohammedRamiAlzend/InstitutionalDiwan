import { httpClient } from '@/shared/api'
import type { MeResponse } from '../model/types'

/** Typed read calls for the current caller's identity. */
export const userApi = {
  me: async (): Promise<MeResponse> => {
    const { data } = await httpClient.get<MeResponse>('/auth/me')
    return data
  },
}
