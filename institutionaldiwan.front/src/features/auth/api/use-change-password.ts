import { useMutation } from '@tanstack/react-query'
import { httpClient, tokenStorage } from '@/shared/api'
import { useSessionStore } from '@/entities/user'
import type { ChangePasswordRequest } from '../model/change-password.schema'
import type { LoginResponse } from '../model/login.schema'

async function changePassword(request: ChangePasswordRequest): Promise<LoginResponse> {
  const { data } = await httpClient.post<LoginResponse>(
    '/auth/change-password',
    request,
  )
  return data
}

/**
 * Changes the caller's password via `POST /api/auth/change-password`. The server
 * issues a fresh JWT, so the token and session are replaced on success.
 */
export function useChangePasswordMutation() {
  const setSession = useSessionStore((state) => state.setSession)

  return useMutation({
    mutationFn: changePassword,
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
