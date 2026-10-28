import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { Loader2 } from 'lucide-react'
import { ROUTES } from '@/shared/config'
import { tokenStorage } from '@/shared/api'
import { readMustChangePassword } from '@/shared/lib'
import { useSessionStore } from '@/entities/user'

/**
 * Guards authenticated routes: no token → `/login`; token but the session is
 * still hydrating (see `AuthSessionLoader`) → spinner; a reset password must be
 * replaced before any other route renders; session ready → child.
 */
export function ProtectedRoute() {
  const location = useLocation()
  const hasUser = useSessionStore((state) => state.user !== null)
  const sessionMustChangePassword = useSessionStore(
    (state) => state.mustChangePassword,
  )
  // The token survives a reload; `/api/auth/me` has no flag, so re-read the claim.
  const mustChangePassword =
    sessionMustChangePassword || readMustChangePassword(tokenStorage.get())

  if (!tokenStorage.has()) {
    return <Navigate to={ROUTES.LOGIN} replace state={{ from: location }} />
  }

  if (!hasUser) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background">
        <Loader2 className="size-8 animate-spin text-accent" aria-hidden />
      </div>
    )
  }

  if (mustChangePassword && location.pathname !== ROUTES.CHANGE_PASSWORD) {
    return <Navigate to={ROUTES.CHANGE_PASSWORD} replace />
  }

  return <Outlet />
}
