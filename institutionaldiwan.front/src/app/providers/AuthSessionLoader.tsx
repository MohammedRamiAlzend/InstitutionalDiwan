import { useEffect, type ReactNode } from 'react'
import { tokenStorage } from '@/shared/api'
import { useMeQuery, useSessionStore } from '@/entities/user'

/**
 * Hydrates the session store from `GET /api/auth/me` on startup when a token
 * exists, so protected routes can render with the caller's permissions.
 * A failed hydration (e.g. an expired token) clears the stale session.
 */
export function AuthSessionLoader({ children }: { children: ReactNode }) {
  const hasUser = useSessionStore((state) => state.user !== null)
  const setSession = useSessionStore((state) => state.setSession)
  const clear = useSessionStore((state) => state.clear)

  const meQuery = useMeQuery({ enabled: tokenStorage.has() && !hasUser })

  useEffect(() => {
    if (meQuery.data) {
      setSession(
        {
          userId: meQuery.data.userId,
          username: meQuery.data.username,
          roles: meQuery.data.roles,
          permissions: meQuery.data.permissions,
        },
        false,
      )
    }
  }, [meQuery.data, setSession])

  useEffect(() => {
    if (meQuery.isError) {
      tokenStorage.clear()
      clear()
    }
  }, [meQuery.isError, clear])

  return <>{children}</>
}
