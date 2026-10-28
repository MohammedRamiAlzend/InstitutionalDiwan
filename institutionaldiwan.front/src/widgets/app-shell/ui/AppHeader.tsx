import { useNavigate } from 'react-router-dom'
import { useSessionStore } from '@/entities/user'
import { useLogout } from '@/features/auth'
import { ROUTES } from '@/shared/config'
import {
  Button,
  DropdownMenu,
  ThemeToggle,
  type MenuEntry,
} from '@/shared/ui'
import {
  ChevronDown,
  KeyRound,
  LogOut,
  Menu,
  MoreHorizontal,
  Settings2,
} from 'lucide-react'

interface AppHeaderProps {
  title: string
  onOpenSidebar: () => void
  onOpenSettings: () => void
  /** Shell-level actions (refresh, density, settings) shared with the chrome menu. */
  menuEntries: readonly MenuEntry[]
}

/** Sticky top bar: navigation trigger, current context, shell actions, theme, and account menu. */
export function AppHeader({
  title,
  onOpenSidebar,
  onOpenSettings,
  menuEntries,
}: AppHeaderProps) {
  const user = useSessionStore((state) => state.user)
  const logout = useLogout()
  const navigate = useNavigate()

  const initial = user?.username.trim().charAt(0).toUpperCase() || '؟'
  const roles = user?.roles.length ? user.roles.join('، ') : 'مستخدم'

  const userMenu: MenuEntry[] = [
    {
      id: 'change-password',
      label: 'تغيير كلمة المرور',
      icon: <KeyRound className="size-4" aria-hidden />,
      onSelect: () => navigate(ROUTES.CHANGE_PASSWORD),
    },
    {
      id: 'display-settings',
      label: 'إعدادات العرض',
      icon: <Settings2 className="size-4" aria-hidden />,
      onSelect: onOpenSettings,
    },
    { type: 'separator' },
    {
      id: 'logout',
      label: 'تسجيل الخروج',
      icon: <LogOut className="size-4 rtl:-scale-x-100" aria-hidden />,
      danger: true,
      onSelect: logout,
    },
  ]

  return (
    <header className="sticky top-0 z-30 flex h-16 items-center gap-2 border-b border-border bg-background/95 px-4 backdrop-blur sm:gap-3 sm:px-6">
      <Button
        type="button"
        variant="ghost"
        size="icon"
        className="size-9 lg:hidden"
        onClick={onOpenSidebar}
        aria-label="فتح القائمة"
      >
        <Menu className="size-5" aria-hidden />
      </Button>

      <div className="flex min-w-0 flex-col">
        <p className="text-[11px] text-muted">الديوان المؤسسي</p>
        <p className="truncate font-heading text-sm font-semibold text-foreground">
          {title}
        </p>
      </div>

      <div className="ms-auto flex items-center gap-1.5 sm:gap-2">
        <DropdownMenu
          trigger={
            <Button
              type="button"
              variant="ghost"
              size="icon"
              aria-label="إجراءات مساحة العمل"
              title="إجراءات مساحة العمل"
            >
              <MoreHorizontal className="size-5" aria-hidden />
            </Button>
          }
          entries={menuEntries}
          label="مساحة العمل"
        />

        <ThemeToggle />

        {user && (
          <DropdownMenu
            trigger={
              <button
                type="button"
                aria-label="قائمة الحساب"
                className="flex cursor-pointer items-center gap-2 rounded-full border border-transparent p-1 transition-colors hover:bg-surface-muted"
              >
                <span className="flex size-8 shrink-0 items-center justify-center rounded-full bg-accent text-sm font-bold text-accent-contrast">
                  {initial}
                </span>
                <span className="hidden min-w-0 flex-col items-start leading-tight sm:flex">
                  <span className="max-w-32 truncate text-xs font-semibold text-foreground">
                    {user.username}
                  </span>
                  <span className="max-w-32 truncate text-[10px] text-muted">{roles}</span>
                </span>
                <ChevronDown className="hidden size-3.5 text-muted sm:block" aria-hidden />
              </button>
            }
            entries={userMenu}
          />
        )}
      </div>
    </header>
  )
}
