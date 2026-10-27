import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { Loader2 } from 'lucide-react'
import { ROUTES } from '@/shared/config'
import { tokenStorage } from '@/shared/api'
import { useSessionStore } from '@/entities/user'

/**
 * Guards authenticated routes: no token → `/login`; token but the session is
 * still hydrating (see `AuthSessionLoader`) → spinner; session ready → child.
 */
export function ProtectedRoute() {
  const location = useLocation()
  const hasUser = useSessionStore((state) => state.user !== null)

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

  return <Outlet />
}
