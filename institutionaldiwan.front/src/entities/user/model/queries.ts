import { useQuery, type UseQueryOptions } from '@tanstack/react-query'
import { tokenStorage } from '@/shared/api'
import type { PagedResult } from '@/shared/types'
import { userApi } from '../api/userApi'
import type {
  MeResponse,
  UserEffectivePermissionsResponse,
  UserListParams,
  UserOption,
  UserOptionParams,
  UserResponse,
} from './types'

export const userKeys = {
  all: ['users'] as const,
  me: () => [...userKeys.all, 'me'] as const,
  lists: () => [...userKeys.all, 'list'] as const,
  list: (params: UserListParams) => [...userKeys.lists(), params] as const,
  optionLists: () => [...userKeys.all, 'options'] as const,
  options: (params: UserOptionParams) => [...userKeys.optionLists(), params] as const,
  detail: (userId: string) => [...userKeys.all, 'detail', userId] as const,
  effectivePermissions: (userId: string) =>
    [...userKeys.all, 'effective-permissions', userId] as const,
}

type EnabledOptions<T> = Partial<Pick<UseQueryOptions<T>, 'enabled'>>

/**
 * Hydrates the session store with `GET /api/auth/me`. Runs only while a token
 * exists and no session is loaded; `AuthSessionLoader` in `src/app/providers`
 * owns the store sync.
 */
export function useMeQuery(options?: EnabledOptions<MeResponse>) {
  return useQuery({
    queryKey: userKeys.me(),
    queryFn: userApi.me,
    enabled: tokenStorage.has(),
    staleTime: 5 * 60 * 1000,
    retry: false,
    ...options,
  })
}

/** Paged users with username search (requires `USER_MANAGE`). */
export function useUsers(
  params: UserListParams,
  options?: EnabledOptions<PagedResult<UserResponse>>,
) {
  return useQuery({
    queryKey: userKeys.list(params),
    queryFn: () => userApi.list(params),
    placeholderData: (previous) => previous,
    staleTime: 60 * 1000,
    ...options,
  })
}

export function useUserById(
  userId: string | null | undefined,
  options?: EnabledOptions<UserResponse>,
) {
  return useQuery({
    queryKey: userKeys.detail(userId ?? ''),
    queryFn: () => userApi.byId(userId as string),
    staleTime: 60 * 1000,
    ...options,
    enabled: Boolean(userId) && (options?.enabled ?? true),
  })
}

export function useUserEffectivePermissions(
  userId: string | null | undefined,
  options?: EnabledOptions<UserEffectivePermissionsResponse>,
) {
  return useQuery({
    queryKey: userKeys.effectivePermissions(userId ?? ''),
    queryFn: () => userApi.effectivePermissions(userId as string),
    staleTime: 60 * 1000,
    retry: false,
    ...options,
    enabled: Boolean(userId) && (options?.enabled ?? true),
  })
}

/**
 * Minimal user labels for pickers (`USER_MANAGE`, `ACCESS_GRANT_MANAGE`, or
 * `DOCUMENT_EDIT`). Inactive accounts are returned so existing references still
 * resolve; filter them out before offering a new selection.
 */
export function useUserOptions(
  params: UserOptionParams,
  options?: EnabledOptions<PagedResult<UserOption>>,
) {
  return useQuery({
    queryKey: userKeys.options(params),
    queryFn: () => userApi.options(params),
    placeholderData: (previous) => previous,
    staleTime: 5 * 60 * 1000,
    ...options,
  })
}
