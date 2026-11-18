import { useLocation, useNavigate } from 'react-router-dom'
import { ClipboardList, History, ShieldCheck } from 'lucide-react'
import { BrandMark, BrandName } from '@/entities/branding'
import { ROUTES } from '@/shared/config'
import { LoginForm, type LoginResponse } from '@/features/auth'
import { Label, ThemeToggle } from '@/shared/ui'

const features = [
  {
    id: 'registry',
    icon: ClipboardList,
    titleKey: 'auth.login.feature.registry-title',
    textKey: 'auth.login.feature.registry-text',
  },
  {
    id: 'permissions',
    icon: ShieldCheck,
    titleKey: 'auth.login.feature.permissions-title',
    textKey: 'auth.login.feature.permissions-text',
  },
  {
    id: 'audit',
    icon: History,
    titleKey: 'auth.login.feature.audit-title',
    textKey: 'auth.login.feature.audit-text',
  },
] as const

export function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const from = (location.state as { from?: { pathname: string } } | null)?.from
    ?.pathname

  const handleSuccess = (session: LoginResponse) => {
    navigate(
      session.mustChangePassword
        ? ROUTES.CHANGE_PASSWORD
        : (from ?? ROUTES.DASHBOARD),
      { replace: true },
    )
  }

  return (
    <div className="flex min-h-screen flex-col bg-background">
      <header className="flex items-center justify-between px-6 py-4 sm:px-10">
        <div className="flex items-center gap-3">
          <BrandMark />
          <div className="leading-tight">
            <p className="font-heading text-lg font-bold text-foreground">
              <BrandName />
            </p>
            <p className="text-xs text-muted"><Label k="auth.login.portal" /></p>
          </div>
        </div>
        <ThemeToggle />
      </header>

      <main className="flex flex-1 items-center justify-center px-6 pb-10">
        <div className="grid w-full max-w-6xl gap-8 lg:grid-cols-2">
          {/* Authentication card — inline-start side (right in RTL). */}
          <section className="flex flex-col justify-center rounded-2xl border border-border bg-surface p-8 shadow-sm sm:p-10">
            <div className="mb-8 flex flex-col gap-2">
              <h1 className="font-heading text-2xl font-bold text-foreground">
                <Label k="auth.login.submit" />
              </h1>
              <p className="text-sm text-muted">
                <Label k="auth.login.intro" />
              </p>
            </div>

            <LoginForm onSuccess={handleSuccess} />

            <p className="mt-8 border-t border-border pt-4 text-center text-xs leading-relaxed text-muted">
              <Label k="auth.login.audit-note" />
            </p>
          </section>

          {/* Institutional brand panel — inline-end side (left in RTL). */}
          <aside className="relative hidden overflow-hidden rounded-2xl bg-accent p-10 text-accent-contrast lg:flex lg:flex-col lg:justify-center">
            <div
              aria-hidden
              className="pointer-events-none absolute -top-24 -end-24 size-72 rounded-full border border-accent-contrast/20"
            />
            <div
              aria-hidden
              className="pointer-events-none absolute -top-10 -end-10 size-44 rounded-full border border-accent-contrast/15"
            />
            <div
              aria-hidden
              className="pointer-events-none absolute -bottom-32 -start-16 size-80 rounded-full border border-accent-contrast/10"
            />

            <div className="relative flex flex-col gap-10">
              <div className="flex flex-col gap-3">
                <p className="text-sm font-medium opacity-80">
                  <Label k="auth.login.country" />
                </p>
                <h2 className="font-heading text-3xl font-bold">
                  <BrandName />
                </h2>
                <p className="text-base opacity-90">
                  <Label k="auth.login.tagline" />
                </p>
              </div>

              <ul className="flex flex-col gap-6">
                {features.map(({ id, icon: Icon, titleKey, textKey }) => (
                  <li key={id} className="flex items-start gap-4">
                    <span className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-accent-contrast/15">
                      <Icon className="size-5" aria-hidden />
                    </span>
                    <div>
                      <p className="font-semibold"><Label k={titleKey} /></p>
                      <p className="text-sm opacity-80"><Label k={textKey} /></p>
                    </div>
                  </li>
                ))}
              </ul>

              <p className="border-t border-accent-contrast/20 pt-4 text-xs opacity-70">
                <Label k="auth.login.panel-note" />
              </p>
            </div>
          </aside>
        </div>
      </main>
    </div>
  )
}
