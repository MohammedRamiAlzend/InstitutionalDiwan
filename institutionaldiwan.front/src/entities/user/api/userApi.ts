import { httpClient } from '@/shared/api'
import type { PagedResult } from '@/shared/types'
import type {
  MeResponse,
  UserEffectivePermissionsResponse,
  UserListParams,
  UserOption,
  UserOptionParams,
  UserResponse,
} from '../model/types'

/** Typed read calls for the caller's identity and the user admin screens. */
export const userApi = {
  me: async (): Promise<MeResponse> => {
    const { data } = await httpClient.get<MeResponse>('/auth/me')
    return data
  },

  list: async (params: UserListParams): Promise<PagedResult<UserResponse>> => {
    const { data } = await httpClient.get<PagedResult<UserResponse>>('/users', {
      params: {
        page: params.page,
        pageSize: params.pageSize,
        search: params.search?.trim() || undefined,
        sortBy: params.sortBy,
        sortDescending: params.sortDescending,
      },
    })
    return data
  },

  byId: async (userId: string): Promise<UserResponse> => {
    const { data } = await httpClient.get<UserResponse>(`/users/${userId}`)
    return data
  },

  effectivePermissions: async (
    userId: string,
  ): Promise<UserEffectivePermissionsResponse> => {
    const { data } = await httpClient.get<UserEffectivePermissionsResponse>(
      `/users/${userId}/effective-permissions`,
    )
    return data
  },

  options: async (params: UserOptionParams): Promise<PagedResult<UserOption>> => {
    const { data } = await httpClient.get<PagedResult<UserOption>>(
      '/identity-options/users',
      {
        params: {
          page: params.page,
          pageSize: params.pageSize,
          search: params.search?.trim() || undefined,
        },
      },
    )
    return data
  },
}
