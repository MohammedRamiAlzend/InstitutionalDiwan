import { useMutation, useQueryClient } from '@tanstack/react-query'
import { httpClient, tokenStorage } from '@/shared/api'
import { useSessionStore } from '@/entities/user'
import type { LoginRequest, LoginResponse } from '../model/login.schema'

async function login(request: LoginRequest): Promise<LoginResponse> {
  const { data } = await httpClient.post<LoginResponse>('/auth/login', request)
  return data
}

/**
 * Authenticates against `POST /api/auth/login`, persists the JWT, and hydrates
 * the session store. The caller decides where to navigate afterwards.
 */
export function useLoginMutation() {
  const setSession = useSessionStore((state) => state.setSession)
  const clearSession = useSessionStore((state) => state.clear)
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: login,
    onMutate: async () => {
      tokenStorage.clear()
      clearSession()
      await queryClient.cancelQueries()
      queryClient.clear()
    },
    onSuccess: async (data) => {
      await queryClient.cancelQueries()
      queryClient.clear()
      tokenStorage.set(data.accessToken)
      setSession(
        {
          userId: data.userId,
          username: data.username,
          roles: data.roles,
          permissions: data.permissions,
        },
        data.mustChangePassword,
      )
    },
  })
}
