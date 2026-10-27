import { useMutation } from '@tanstack/react-query'
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

  return useMutation({
    mutationFn: login,
    onSuccess: (data) => {
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
