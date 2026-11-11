import { useQuery } from '@tanstack/react-query'
import { brandingApi } from '../api/brandingApi'

export const brandingKeys = {
  all: ['branding'] as const,
  detail: () => [...brandingKeys.all, 'detail'] as const,
}

/** The institution name and logo version shown across the application. */
export function useBranding() {
  return useQuery({
    queryKey: brandingKeys.detail(),
    queryFn: brandingApi.get,
    staleTime: 5 * 60 * 1000,
    retry: 1,
  })
}
