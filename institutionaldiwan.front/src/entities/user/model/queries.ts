import { useQuery, type UseQueryOptions } from '@tanstack/react-query'
import { tokenStorage } from '@/shared/api'
import { userApi } from '../api/userApi'
import type { MeResponse } from './types'

export const userKeys = {
  all: ['users'] as const,
  me: () => [...userKeys.all, 'me'] as const,
}

/**
 * Hydrates the session store with `GET /api/auth/me`. Runs only while a token
 * exists and no session is loaded; `AuthSessionLoader` in `src/app/providers`
 * owns the store sync.
 */
export function useMeQuery(
  options?: Partial<Pick<UseQueryOptions<MeResponse>, 'enabled'>>,
) {
  return useQuery({
    queryKey: userKeys.me(),
    queryFn: userApi.me,
    enabled: tokenStorage.has(),
    staleTime: 5 * 60 * 1000,
    retry: false,
    ...options,
  })
}
