import { useEffect, type ReactNode } from 'react'
import { Loader2 } from 'lucide-react'
import { toast } from 'sonner'
import { BrandMark, BrandName } from '@/entities/branding'
import { useUiLabelsQuery } from '@/entities/ui-label'
import { useSessionStore } from '@/entities/user'
import { tokenStorage } from '@/shared/api'
import { getLabelEditMode, setLabelEditMode, useLabelsStore } from '@/shared/lib'

/** Branded full-page loading screen; tokens are resolved early in index.html, so no white flash. */
function BootScreen() {
  return (
    <div
      role="status"
      aria-live="polite"
      className="flex min-h-screen flex-col items-center justify-center gap-4 bg-background px-6 text-center"
    >
      <BrandMark className="size-14" />
      <p className="font-heading text-lg font-bold text-foreground">
        <BrandName />
      </p>
      <div className="flex items-center gap-2 text-sm text-muted">
        <Loader2 className="size-4 animate-spin" aria-hidden />
        <span>جارٍ تجهيز الواجهة…</span>
      </div>
    </div>
  )
}

/**
 * Reveals the app once the UI labels are ready. A returning browser seeds the labels store
 * from its local cache (ready at once, no screen) and revalidates in the background; a first
 * visit shows the boot screen while the overrides load in parallel with `AuthSessionLoader`
 * (the parent, which hydrates `/api/auth/me`). The sign-in page needs no fetch and renders on
 * the defaults. A failed fetch boots on the built-in defaults with a non-blocking warning —
 * labels are a convenience, never a functional barrier.
 */
export function LabelsBootGate({ children }: { children: ReactNode }) {
  // Subscribe to the session so login/logout re-evaluates token presence (tokenStorage is not reactive).
  const user = useSessionStore((state) => state.user)
  const hasToken = tokenStorage.has()
  const ready = useLabelsStore((state) => state.ready)
  const applySnapshot = useLabelsStore((state) => state.applySnapshot)
  const markReady = useLabelsStore((state) => state.markReady)

  const labelsQuery = useUiLabelsQuery({ enabled: hasToken })

  useEffect(() => {
    if (labelsQuery.data) applySnapshot(labelsQuery.data)
  }, [labelsQuery.data, applySnapshot])

  useEffect(() => {
    if (labelsQuery.isError) {
      markReady()
      if (user) toast.warning('تعذّر تحميل تسميات الواجهة؛ عُرضت الأسماء الافتراضية.')
    }
  }, [labelsQuery.isError, markReady, user])

  // Edit mode is a superadmin-only capability. Force it off for any other session so a
  // leftover per-browser preference never leaks editable labels to a normal user.
  useEffect(() => {
    if (user && user.username !== 'superadmin' && getLabelEditMode() === 'on') {
      setLabelEditMode('off')
    }
  }, [user])

  if (hasToken && !ready) return <BootScreen />
  return <>{children}</>
}