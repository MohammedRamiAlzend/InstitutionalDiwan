import { useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQueryClient } from '@tanstack/react-query'
import { tokenStorage } from '@/shared/api'
import { ROUTES } from '@/shared/config'
import { useSessionStore, userKeys } from '@/entities/user'

/**
 * Clears the JWT, session store, and cached user data, then returns to `/login`.
 * Shared by the logout button and the shell user menu.
 */
export function useLogout() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const clear = useSessionStore((state) => state.clear)

  return useCallback(() => {
    tokenStorage.clear()
    clear()
    queryClient.removeQueries({ queryKey: userKeys.all })
    navigate(ROUTES.LOGIN, { replace: true })
  }, [clear, navigate, queryClient])
}
