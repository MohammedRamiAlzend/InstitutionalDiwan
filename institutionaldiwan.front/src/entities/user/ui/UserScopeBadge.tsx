import { Globe2, Network, Building2 } from 'lucide-react'
import { cn } from '@/shared/lib'
import type { UserScope } from '../model/types'

const scopeMeta: Record<UserScope, { label: string; icon: typeof Globe2 }> = {
  System: { label: 'نطاق النظام', icon: Globe2 },
  Department: { label: 'نطاق القسم', icon: Building2 },
  Subtree: { label: 'شجرة الأقسام', icon: Network },
}

interface UserScopeBadgeProps {
  scope: UserScope
  className?: string
}

export function UserScopeBadge({ scope, className }: UserScopeBadgeProps) {
  const { label, icon: Icon } = scopeMeta[scope]

  return (
    <span
      className={cn(
        'inline-flex items-center gap-1 rounded-full bg-surface-muted px-2.5 py-0.5 text-xs font-medium text-muted',
        className,
      )}
    >
      <Icon className="size-3.5" aria-hidden />
      {label}
    </span>
  )
}
