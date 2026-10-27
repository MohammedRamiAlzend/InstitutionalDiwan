import { LogoutButton } from '@/features/auth'
import { ThemeToggle } from '@/shared/ui'
import { useSessionStore } from '@/entities/user'

/**
 * Authenticated landing page for Module 01. The full KPI / recent-activity
 * dashboard (documents, movements) lands with the Correspondence module.
 */
export function DashboardPage() {
  const user = useSessionStore((state) => state.user)

  return (
    <div className="flex min-h-screen flex-col bg-background">
      <header className="flex items-center justify-between border-b border-border px-6 py-4">
        <p className="font-heading text-lg font-bold text-foreground">
          الديوان المؤسسي
        </p>
        <div className="flex items-center gap-3">
          <ThemeToggle />
          <LogoutButton size="sm" />
        </div>
      </header>

      <main className="flex flex-1 flex-col items-center justify-center gap-4 px-6 text-center">
        <h1 className="font-heading text-2xl font-bold text-foreground">
          مرحبًا، {user?.username}
        </h1>
        <p className="max-w-md text-sm text-muted">
          تم تسجيل دخولك بنجاح. تُبنى لوحة التحكم التفصيلية (الوثائق،
          الحركات، التدقيق) مع تفعيل الوحدات اللاحقة.
        </p>
        <p className="text-xs text-muted">
          الصلاحيات الممنوحة:{' '}
          <span dir="ltr" className="font-medium">
            {user?.permissions.length ?? 0}
          </span>
        </p>
      </main>
    </div>
  )
}
