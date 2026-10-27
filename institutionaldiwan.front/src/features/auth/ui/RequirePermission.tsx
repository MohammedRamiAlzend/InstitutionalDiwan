import type { ReactNode } from 'react'
import { ShieldAlert } from 'lucide-react'
import type { PermissionCode } from '@/shared/config'
import { useSessionStore } from '@/entities/user'
interface RequirePermissionProps {
  permission: PermissionCode
  /** Optional explicit permission list override (defaults to the session store). */
  permissions?: readonly PermissionCode[]
  children: ReactNode
}

/** Arabic-first 403 state rendered when the caller lacks the permission. */
export function ForbiddenState() {
  return (
    <div className="flex min-h-64 flex-col items-center justify-center gap-3 text-center">
      <ShieldAlert className="size-10 text-warning" aria-hidden />
      <h2 className="text-xl font-bold text-foreground">لا تملك صلاحية الوصول</h2>
      <p className="max-w-md text-sm text-muted">
        هذه الصفحة مخصصة لمستخدمين مصرح لهم بصلاحية مختلفة. إذا كنت بحاجة إلى
        الوصول، تواصل مع مشرف النظام.
      </p>
    </div>
  )
}

/** Renders `children` only when the caller holds the required permission. */
export function RequirePermission({
  permission,
  permissions,
  children,
}: RequirePermissionProps) {
  const sessionUser = useSessionStore((state) => state.user)
  const effective = permissions ?? sessionUser?.permissions ?? []

  if (!effective.includes(permission)) {
    return <ForbiddenState />
  }

  return <>{children}</>
}
