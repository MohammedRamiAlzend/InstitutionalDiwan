import { Link, useNavigate } from 'react-router-dom'
import { KeyRound, Landmark, ShieldAlert } from 'lucide-react'
import { ROUTES } from '@/shared/config'
import { useSessionStore } from '@/entities/user'
import { ChangePasswordForm } from '@/features/auth'
import { ThemeToggle } from '@/shared/ui'

/** `/change-password` — signed-in password change, forced after a reset login. */
export function ChangePasswordPage() {
  const navigate = useNavigate()
  const mustChangePassword = useSessionStore((state) => state.mustChangePassword)

  return (
    <div className="flex min-h-screen flex-col bg-background">
      <header className="flex items-center justify-between px-6 py-4 sm:px-10">
        <div className="flex items-center gap-3">
          <span className="flex size-10 items-center justify-center rounded-full bg-accent text-accent-contrast">
            <Landmark className="size-5" aria-hidden />
          </span>
          <div className="leading-tight">
            <p className="font-heading text-lg font-bold text-foreground">
              الديوان المؤسسي
            </p>
            <p className="text-xs text-muted">الحساب والأمان</p>
          </div>
        </div>
        <ThemeToggle />
      </header>

      <main className="flex flex-1 items-center justify-center px-6 pb-16">
        <section className="w-full max-w-md rounded-2xl border border-border bg-surface p-8 shadow-sm">
          <span className="mb-5 flex size-11 items-center justify-center rounded-full bg-accent text-accent-contrast">
            <KeyRound className="size-5" aria-hidden />
          </span>

          <h1 className="font-heading text-2xl font-bold text-foreground">
            تغيير كلمة المرور
          </h1>
          <p className="mt-2 text-sm text-muted">
            أدخل كلمة المرور الحالية، ثم اختر كلمة مرور جديدة لا تقل عن ثمانية
            أحرف وتختلف عن الحالية.
          </p>

          {mustChangePassword && (
            <p
              role="status"
              className="mt-4 flex items-start gap-2 rounded-lg bg-warning/10 px-3 py-2 text-sm text-warning"
            >
              <ShieldAlert className="mt-0.5 size-4 shrink-0" aria-hidden />
              يجب تغيير كلمة المرور قبل متابعة استخدام النظام.
            </p>
          )}

          <div className="mt-6">
            <ChangePasswordForm
              onSuccess={() => navigate(ROUTES.DASHBOARD, { replace: true })}
            />
          </div>

          {!mustChangePassword && (
            <p className="mt-6 border-t border-border pt-4 text-center text-xs text-muted">
              <Link
                to={ROUTES.DASHBOARD}
                className="font-medium text-accent hover:underline"
              >
                العودة إلى لوحة التحكم
              </Link>
            </p>
          )}
        </section>
      </main>
    </div>
  )
}
