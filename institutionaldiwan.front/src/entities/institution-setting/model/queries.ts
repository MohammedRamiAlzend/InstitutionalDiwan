import { useQuery, type UseQueryOptions } from '@tanstack/react-query'
import { institutionSettingApi } from '../api/institutionSettingApi'
import type { InstitutionSettingResponse } from './types'

export const institutionSettingKeys = {
  all: ['institution-settings'] as const,
  detail: () => [...institutionSettingKeys.all, 'detail'] as const,
}

type EnabledOptions<T> = Partial<Pick<UseQueryOptions<T>, 'enabled'>>

/** The singleton settings row (requires `DEPARTMENT_MANAGE`). */
export function useInstitutionSettings(
  options?: EnabledOptions<InstitutionSettingResponse>,
) {
  return useQuery({
    queryKey: institutionSettingKeys.detail(),
    queryFn: institutionSettingApi.get,
    staleTime: 60 * 1000,
    ...options,
  })
}
